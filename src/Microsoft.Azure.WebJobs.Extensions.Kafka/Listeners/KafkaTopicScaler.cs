// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTopicScaler<TKey, TValue>
    {
        private readonly string topic;
        private readonly ILogger logger;
        private readonly AdminClientConfig adminClientConfig;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly Lazy<List<TopicPartition>> topicPartitions;

        public KafkaTopicScaler(string topic, ILogger logger, IConsumer<TKey, TValue> consumer, AdminClientConfig adminClientConfig)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Invalid topic", nameof(topic));
            }
            
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.adminClientConfig = adminClientConfig ?? throw new ArgumentNullException(nameof(adminClientConfig));
            this.consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            this.topic = topic;
            this.topicPartitions = new Lazy<List<TopicPartition>>(LoadTopicPartitions);
        }

        private List<TopicPartition> LoadTopicPartitions()
        {
            try
            {
                var timeout = TimeSpan.FromSeconds(5);
                using var adminClient = new AdminClientBuilder(adminClientConfig).Build();
                var metadata = adminClient.GetMetadata(this.topic, timeout);
                if (metadata.Topics == null || metadata.Topics.Count == 0)
                {
                    logger.LogError("Could not load metadata information about topic '{topic}'", this.topic);
                    return new List<TopicPartition>();
                }

                var topicMetadata = metadata.Topics[0];
                var partitions = topicMetadata.Partitions;
                if (partitions == null || partitions.Count == 0)
                {
                    logger.LogError("Could not load partition information about topic '{topic}'", this.topic);
                    return new List<TopicPartition>();
                }

                return partitions.Select(x => new TopicPartition(topicMetadata.Topic, new Partition(x.PartitionId))).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to load partition information from topic '{this.topic}'");
            }

            return new List<TopicPartition>();
        }

        /// <summary>
        /// For debugging purposes
        /// Logs the current lag in the topic
        /// </summary>
        internal (long TotalLag, long PartitionCount) ReportLag()
        {
            var operationTimeout = TimeSpan.FromSeconds(5);
            var allPartitions = topicPartitions.Value;
            if (allPartitions == null)
            {
                return (0L, 0L);
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
                logger.LogInformation("Total lag in '{topic}' is {totalLag}, highest partition lag found in {partition} with value of {offsetDifference}", this.topic, totalLag, partitionWithHighestLag.Value, highestPartitionLag);
            }
        
            return (totalLag, allPartitions.Count);
        }
    }
}