// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Batch kafka message publisher.
    /// </summary>
    public class BatchKafkaMessagePublisher<TKey, TValue> : IKafkaMessagePublisher<TKey, TValue>
    {
        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<int, PartitionPublisher<TKey, TValue>> itemsByPartition = new ConcurrentDictionary<int, PartitionPublisher<TKey, TValue>>();

        public int MaxBatchSize { get; set; } = 64;

        public TimeSpan MaxClientTimeout { get; set; } = TimeSpan.FromSeconds(60);


        public BatchKafkaMessagePublisher(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, int maxBatchSize, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.logger = logger;
            this.MaxBatchSize = maxBatchSize;
        }

        public void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            var partitionPublisher = this.itemsByPartition.GetOrAdd(consumeResult.Partition, (partition) =>
            {
                return new PartitionPublisher<TKey, TValue>(this.executor, this.consumer, this.MaxBatchSize, this.MaxClientTimeout, cancellationToken, this.logger);
            });

            partitionPublisher.Publish(consumeResult);
        }

        /// <summary>
        /// Clears the partitions. Called when they have been revoked
        /// </summary>
        /// <param name="partitions">Partitions.</param>
        public void ClearPartitions(IList<TopicPartition> partitions)
        {
            foreach (var partition in partitions)
            {
                if (this.itemsByPartition.TryRemove(partition.Partition, out var removedPartition))
                {
                    removedPartition.Dispose();
                }
            }
        }

        public void AddPartitions(IList<TopicPartition> partitions)
        {
            // Do nothing, as partition will be created automatically when a new message arrives
        }
    }
}