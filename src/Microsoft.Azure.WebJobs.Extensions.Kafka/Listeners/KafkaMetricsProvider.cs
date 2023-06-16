// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using static Confluent.Kafka.ConfigPropertyNames;


namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaMetricsProvider<TKey, TValue>
    {
        private readonly string topicName;
        private readonly AdminClientConfig adminClientConfig;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly ILogger logger;
        private readonly Lazy<List<TopicPartition>> topicPartitions;

        public KafkaMetricsProvider(string topicName, AdminClientConfig adminClientConfig, IConsumer<TKey, TValue> consumer, ILogger logger)
        {
            this.topicName = topicName;
            this.adminClientConfig = adminClientConfig;
            this.logger = logger;
            this.consumer = consumer;
            this.topicPartitions = new Lazy<List<TopicPartition>>(LoadTopicPartitions);
        }

        public Task<KafkaTriggerMetrics> GetMetricsAsync()
        {
            var startTime = DateTime.UtcNow;
            this.logger.LogInformation($"Getting metrics at time {startTime}:");
            var allPartitions = topicPartitions.Value;
            if (allPartitions == null)
            {
                // returns null
                return Task.FromResult(new KafkaTriggerMetrics(0L, 0));
            }

            var operationTimeout = TimeSpan.FromSeconds(5);

            // get the parameters required for kafkatriggermetrics
            long totalLag = GetTotalLag(allPartitions, operationTimeout);
            int paritionCount = allPartitions.Count;
            
            var endTime = DateTime.UtcNow;
            this.logger.LogInformation($"Ended getting metrics at time {endTime}. Time taken: {endTime - startTime}.");

            return Task.FromResult(new KafkaTriggerMetrics(totalLag, paritionCount));
        }

        protected virtual List<TopicPartition> LoadTopicPartitions()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
                var metadata = adminClient.GetMetadata(this.topicName, timeout);
                if (metadata.Topics == null || metadata.Topics.Count == 0)
                {
                    logger.LogError($"Could not load metadata information about topic '{this.topicName}'");
                    return new List<TopicPartition>();
                }

                var topicMetadata = metadata.Topics[0];
                var partitions = topicMetadata.Partitions;
                if (partitions == null || partitions.Count == 0)
                {
                    logger.LogError($"Could not load partition information about topic '{this.topicName}'");
                    return new List<TopicPartition>();
                }

                return partitions.Select(x => new TopicPartition(topicMetadata.Topic, new Partition(x.PartitionId))).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load partition information from topic '{this.topicName}'");
            }
            var endTime = DateTime.UtcNow;
            logger.LogInformation($"AdminClient takes {endTime - startTime} to get paritions");
            return new List<TopicPartition>();
        }


        private long GetTotalLag(List<TopicPartition> allPartitions, TimeSpan operationTimeout)
        {
            var startTime = DateTime.UtcNow;
            long totalLag = 0;
            var ownedCommittedOffset = consumer.Committed(allPartitions, operationTimeout);
            var partitionWithHighestLag = Partition.Any;
            long highestPartitionLag = 0L;

            foreach (var topicPartition in allPartitions)
            {
                // This call goes to the server always which probably yields the most accurate results. It blocks.
                // Alternatively we could use consumer.GetWatermarkOffsets() that returns cached values, without blocking.
                
                var watermark = consumer.GetWatermarkOffsets(topicPartition);

                var commited = ownedCommittedOffset.FirstOrDefault(x => x.Partition == topicPartition.Partition);
                if (commited != null)
                {
                    long diff;
                    if (commited.Offset == Offset.Unset)
                    {
                        diff = watermark.High.Value;
                        this.logger.LogInformation($"For the partition {topicPartition}, high watermark: ({watermark.High}), low watermark: ({watermark.Low}), committed offset: (unset), lag for partition: {diff}");
                    }
                    else
                    {
                        diff = watermark.High.Value - commited.Offset.Value;
                        this.logger.LogInformation($"For the partition {topicPartition}, high watermark: ({watermark.High}), low watermark: ({watermark.Low}), committed offset: ({commited.Offset.Value}), lag for partition: {diff}", null);
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
                // highestPartitionLag is the offsetDifference
                logger.LogInformation($"Total lag in '{this.topicName}' is {totalLag}, highest partition lag found in {partitionWithHighestLag.Value} with value of {highestPartitionLag}.");
            }
            var endTime = DateTime.UtcNow;
            this.logger.LogInformation($"Consumer takes {endTime - startTime} for the watermark offset calls.");
            return totalLag;
        }
    }
}