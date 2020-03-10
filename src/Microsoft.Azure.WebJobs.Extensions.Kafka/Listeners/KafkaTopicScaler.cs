﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTopicScaler<TKey, TValue>
    {
        private readonly string topicName;
        private readonly string consumerGroup;
        private readonly ILogger logger;
        private readonly AdminClientConfig adminClientConfig;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly Lazy<List<TopicPartition>> topicPartitions;

        public ScaleMonitorDescriptor Descriptor { get; }

        public KafkaTopicScaler(string topic, string consumerGroup, ScaleMonitorDescriptor scaleMonitorDescriptor, IConsumer<TKey, TValue> consumer, AdminClientConfig adminClientConfig, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Invalid topic", nameof(topic));
            }

            if (string.IsNullOrWhiteSpace(consumerGroup))
            {
                throw new ArgumentException("Invalid consumer group", nameof(consumerGroup));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.adminClientConfig = adminClientConfig ?? throw new ArgumentNullException(nameof(adminClientConfig));
            this.consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            this.Descriptor = scaleMonitorDescriptor ?? throw new ArgumentNullException(nameof(scaleMonitorDescriptor));
            this.topicName = topic;            
            this.topicPartitions = new Lazy<List<TopicPartition>>(LoadTopicPartitions);
            this.consumerGroup = consumerGroup;
        }

        protected virtual List<TopicPartition> LoadTopicPartitions()
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
                var metadata = adminClient.GetMetadata(this.topicName, timeout);
                if (metadata.Topics == null || metadata.Topics.Count == 0)
                {
                    logger.LogError("Could not load metadata information about topic '{topic}'", this.topicName);
                    return new List<TopicPartition>();
                }

                var topicMetadata = metadata.Topics[0];
                var partitions = topicMetadata.Partitions;
                if (partitions == null || partitions.Count == 0)
                {
                    logger.LogError("Could not load partition information about topic '{topic}'", this.topicName);
                    return new List<TopicPartition>();
                }

                return partitions.Select(x => new TopicPartition(topicMetadata.Topic, new Partition(x.PartitionId))).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load partition information from topic '{topic}'", this.topicName);
            }

            return new List<TopicPartition>();
        }

        public Task<KafkaTriggerMetrics> GetMetricsAsync()
        {
            var operationTimeout = TimeSpan.FromSeconds(5);
            var allPartitions = topicPartitions.Value;
            if (allPartitions == null)
            {
                return Task.FromResult(new KafkaTriggerMetrics(0L, 0));
            }

            var ownedCommittedOffset = consumer.Committed(allPartitions, operationTimeout);
            var partitionWithHighestLag = Partition.Any;
            long highestPartitionLag = 0L;
            long totalLag = 0L;
            foreach (var topicPartition in allPartitions)
            {
                // This call goes to the server always which probably yields the most accurate results. It blocks.
                // Alternatively we could use consumer.GetWatermarkOffsets() that returns cached values, without blocking.
                var watermark = consumer.QueryWatermarkOffsets(topicPartition, operationTimeout);

                var commited = ownedCommittedOffset.FirstOrDefault(x => x.Partition == topicPartition.Partition);
                if (commited != null)
                {
                    long diff;
                    if (commited.Offset == Offset.Unset)
                    {
                        diff = watermark.High.Value;
                    }
                    else
                    {
                        diff = watermark.High.Value - commited.Offset.Value;
                    }

                    totalLag += diff;

                    if (diff > highestPartitionLag)
                    {
                        highestPartitionLag = diff;
                        partitionWithHighestLag = topicPartition.Partition;
                    }
                }
            }

            if (partitionWithHighestLag != Partition.Any)
            {
                logger.LogInformation("Total lag in '{topic}' is {totalLag}, highest partition lag found in {partition} with value of {offsetDifference}", this.topicName, totalLag, partitionWithHighestLag.Value, highestPartitionLag);
            }

            return Task.FromResult(new KafkaTriggerMetrics(totalLag, allPartitions.Count));
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.OfType<KafkaTriggerMetrics>().ToArray());
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<KafkaTriggerMetrics> context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.ToArray());
        }

        private ScaleStatus GetScaleStatusCore(int workerCount, KafkaTriggerMetrics[] metrics)
        {
            var status = new ScaleStatus
            {
                Vote = ScaleVote.None,
            };

            const int NumberOfSamplesToConsider = 5;

            // At least 5 samples are required to make a scale decision for the rest of the checks.
            if (metrics == null || metrics.Length < NumberOfSamplesToConsider)
            {
                return status;
            }

            var lastMetrics = metrics.Last();
            long totalLag = lastMetrics.TotalLag;
            long partitionCount = lastMetrics.PartitionCount;
            long lagThreshold = 1000L;

            if (partitionCount > 0 && partitionCount < workerCount)
            {
                status.Vote = ScaleVote.ScaleIn;

                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation("WorkerCount ({workerCount}) > PartitionCount ({partitionCount}). For topic {topicName}, for consumer group {consumerGroup}.", workerCount, partitionCount, this.topicName, this.consumerGroup);
                    this.logger.LogInformation("Number of instances ({workerCount}) is too high relative to number of partitions ({partitionCount}). For topic {topicName}, for consumer group {consumerGroup}.", workerCount, partitionCount, this.topicName, this.consumerGroup);
                }

                return status;
            }


            // Check to see if the Kafka consumer has been empty for a while. Only if all metrics samples are empty do we scale down.
            bool partitionIsIdle = metrics.All(p => p.TotalLag == 0);

            if (partitionIsIdle)
            {
                status.Vote = ScaleVote.ScaleIn;
                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation("Topic '{topicName}', for consumer group {consumerGroup}' is idle.", this.topicName, this.consumerGroup);
                }

                return status;
            }


            // don't scale out beyond the number of partitions, Maintain a minimum ratio of 1 worker per 1,000 unprocessed messages.
            if (metrics[0].TotalLag > 0 && totalLag / lagThreshold > partitionCount)
            {
                if (workerCount < partitionCount)
                {
                    bool queueLengthIncreasing =
                    IsTrueForLast(
                       metrics,
                       NumberOfSamplesToConsider,
                       (prev, next) => prev.TotalLag < next.TotalLag) && metrics[0].TotalLag > 0;

                    if (queueLengthIncreasing)
                    {
                        status.Vote = ScaleVote.ScaleOut;

                        if (this.logger.IsEnabled(LogLevel.Information))
                        {
                            this.logger.LogInformation("Total lag ({totalLag}) is less than the number of instances ({workerCount}). Scale out, for topic {topicName}, for consumer group {consumerGroup}.", totalLag, workerCount, topicName, consumerGroup);
                        }

                        return status;
                    }
                }
                return status;
            }

            bool queueLengthDecreasing =
                IsTrueForLast(
                    metrics,
                    NumberOfSamplesToConsider,
                    (prev, next) => prev.TotalLag > next.TotalLag);

            if (queueLengthDecreasing)
            {
                status.Vote = ScaleVote.ScaleIn;

                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation("Total lag length is decreasing for topic {topicName}, for consumer group {consumerGroup}.", this.topicName, this.consumerGroup);
                }
            }
            return status;
        }

        private static bool IsTrueForLast(IList<KafkaTriggerMetrics> samples, int count, Func<KafkaTriggerMetrics, KafkaTriggerMetrics, bool> predicate)
        {
            if (samples.Count < count)
            {
                return false;
            }

            // Walks through the list from left to right starting at len(samples) - count.
            for (int i = samples.Count - count; i < samples.Count - 1; i++)
            {
                if (!predicate(samples[i], samples[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}