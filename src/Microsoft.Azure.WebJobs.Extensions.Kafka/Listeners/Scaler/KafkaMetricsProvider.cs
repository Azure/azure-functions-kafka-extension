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
        protected Lazy<List<TopicPartition>> topicPartitions;

        virtual protected internal KafkaTriggerMetrics LastCalculatedMetrics { get; set; }

        internal KafkaMetricsProvider(string topicName, AdminClientConfig adminClientConfig, IConsumer<TKey, TValue> consumer, ILogger logger) : this(topicName, adminClientConfig, logger)
        {
            this.consumer = consumer;
        }

        internal KafkaMetricsProvider(string topicName, AdminClientConfig adminClientConfig, ILogger logger)
        {
            this.topicName = topicName;
            this.adminClientConfig = adminClientConfig;
            this.logger = logger;
            this.topicPartitions = new Lazy<List<TopicPartition>>(LoadTopicPartitions);
            this.LastCalculatedMetrics = null;
        }

        public virtual Task<KafkaTriggerMetrics> GetMetricsAsync()
        {
            var allPartitions = topicPartitions.Value;
            if (allPartitions == null)
            {
                return Task.FromResult(new KafkaTriggerMetrics(0L, 0));
            }

            var operationTimeout = TimeSpan.FromSeconds(5);

            long totalLag = 0;
            try
            {
                totalLag = GetTotalLag(allPartitions, operationTimeout);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to retrieve lag from topic '{this.topicName}'");
            }
            int paritionCount = allPartitions.Count;

            var metrics = new KafkaTriggerMetrics(totalLag, paritionCount);

            // Storing the metrics along with TimeStamp.
            this.LastCalculatedMetrics = metrics;

            return Task.FromResult(metrics);
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
            return new List<TopicPartition>();
        }

        protected virtual List<TopicPartition> LoadAssignedPartitions()
        {
            if (this.consumer != null)
            {
                try
                {
                    var partitions = consumer?.Assignment;
                    if (partitions == null || partitions.Count == 0)
                    {
                        logger.LogError($"Could not load assigned partition information about topic '{this.topicName}'");
                        return new List<TopicPartition>();
                    }

                    return partitions.ToList();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to load assigned partition information from topic '{this.topicName}'");
                }
            }
            return new List<TopicPartition>();
        }

        // Returns the total number of unprocessed messages across all partitions.
        private long GetTotalLag(List<TopicPartition> allPartitions, TimeSpan operationTimeout)
        {
            long totalLag = 0;
            var ownedCommittedOffset = consumer.Committed(allPartitions, operationTimeout);
            var partitionWithHighestLag = Partition.Any;
            long highestPartitionLag = 0L;
            // List of partitions that the consumer is reading from.
            var currentPartitions = LoadAssignedPartitions();
            // List of partitions that the consumer is not reading from.
            var unassignedPartitions = allPartitions.Except(currentPartitions).ToList();

            foreach (var topicPartition in currentPartitions)
            {
                var watermark = consumer.GetWatermarkOffsets(topicPartition);
                var committed = ownedCommittedOffset.FirstOrDefault(x => x.Partition == topicPartition.Partition);

                bool bothWatermarksUnset = watermark.High.Value == Offset.Unset && watermark.Low.Value == Offset.Unset;
                bool lowWatermarkZeroAndCommittedIsUnSet = watermark.Low.Value == 0 && committed.Offset.Value == Offset.Unset;
                // if GetWatermarkOffsets fails to return valid values, use QueryWatermarkOffsets.
                if (bothWatermarksUnset || lowWatermarkZeroAndCommittedIsUnSet)
                {
                    watermark = consumer.QueryWatermarkOffsets(topicPartition, operationTimeout);
                }

                UpdateTotalLag(watermark, committed, ref totalLag, ref partitionWithHighestLag, ref highestPartitionLag);
            }
            foreach (var topicPartition in unassignedPartitions)
            {
                var watermark = consumer.QueryWatermarkOffsets(topicPartition, operationTimeout);
                var committed = ownedCommittedOffset.FirstOrDefault(x => x.Partition == topicPartition.Partition);

                UpdateTotalLag(watermark, committed, ref totalLag, ref partitionWithHighestLag, ref highestPartitionLag);
            }

            // This log is only for customer reference to show calculation of total lag.
            if (partitionWithHighestLag != Partition.Any)
            {
                logger.LogInformation($"Total lag in '{this.topicName}' is {totalLag}, highest partition lag found in {partitionWithHighestLag.Value} with value of {highestPartitionLag}.");
            }
            return totalLag;
        }

        private void UpdateTotalLag(WatermarkOffsets watermark, TopicPartitionOffset committed, ref long totalLag, ref Partition partitionWithHighestLag, ref long highestPartitionLag)
        {
            var diff = GetDiff(watermark, committed);
            totalLag += diff;

            // Update highest partition lag for logging purposes.
            if (diff > highestPartitionLag)
            {
                highestPartitionLag = diff;
                partitionWithHighestLag = committed.Partition;
            }
        }

        private long GetDiff(WatermarkOffsets watermark, TopicPartitionOffset committed)
        {
            long diff = watermark.High.Value - watermark.Low.Value;
            if (committed != null && committed.Offset.Value != Offset.Unset)
            {
                diff = Math.Min(watermark.High.Value - committed.Offset.Value, diff);
            }
            return diff;
        }
    }
}