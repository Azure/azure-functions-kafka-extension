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
        private readonly ILogger logger;
        private readonly Lazy<List<TopicPartition>> topicPartitions;
        // why doesnt iconsumer work LKjfdalksjf
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly string topicName;


        //public KafkaMetricsProvider(Lazy<List<TopicPartition>> topicPartitions, string topicName, IConsumer<TKey, TValue> consumer)
        public KafkaMetricsProvider(Lazy<List<TopicPartition>> topicPartitions, string topicName, ILogger logger, IConsumer<TKey, TValue> consumer)
        {
            this.topicPartitions = topicPartitions;
            this.topicName = topicName;
            this.logger = logger;
            this.consumer = consumer;
        }

        // public KafkaMetricsProvider(string functionID, IEventHubConsumerClient client, ...)
        //async Task<KafkaTriggerMetrics> IScaleMonitor.GetMetricsAsync()
        //{
        //    var res = await Task.Run(() => GetMetricsAsync());
        //    return res;
        //}

        //private KafkaTriggerMetrics CreateKafkaTriggerMetrics()
        //{
        //    return new KafkaTriggerMetrics(1, 2, 3);
        //    //return null;
        //}

        public Task<KafkaTriggerMetrics> GetMetricsAsync()
        {
            // get three parameters required for kafkatriggermetrics
            var operationTimeout = TimeSpan.FromSeconds(5);

            var allPartitions = topicPartitions.Value;
            if (allPartitions == null)
            {
                // returns null
                return Task.FromResult(new KafkaTriggerMetrics(0L, 0, 0));
            }

            long totalLag = GetTotalLag(allPartitions, operationTimeout);
            int paritionCount = allPartitions.Count;
            int eventCount = GetUnprocessedEventCount();

            return Task.FromResult(new KafkaTriggerMetrics(totalLag, paritionCount, eventCount));
        }

        private long GetTotalLag(List<TopicPartition> allPartitions, TimeSpan operationTimeout)
        {
            long totalLag = 0;
            var ownedCommittedOffset = consumer.Committed(allPartitions, operationTimeout);
            var partitionWithHighestLag = Partition.Any;
            long highestPartitionLag = 0L;

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
                // highestPartitionLag is the offsetDifference
                logger.LogInformation($"Total lag in '{this.topicName}' is {totalLag}, highest partition lag found in {partitionWithHighestLag.Value} with value of {highestPartitionLag}.");
            }
            return totalLag;
        }
    
        private int GetUnprocessedEventCount()
        {
            // logic to get unprocessed event count
            return 1;
        }
    }
}