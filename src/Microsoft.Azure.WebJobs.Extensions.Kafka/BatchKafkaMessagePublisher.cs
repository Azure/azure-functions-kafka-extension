using System;
using System.Collections.Concurrent;
using System.Threading;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Batch kafka message publisher.
    /// </summary>
    public partial class BatchKafkaMessagePublisher<TKey, TValue> : IKafkaMessagePublisher<TKey, TValue>
    {
        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly ILogger logger;

        public int MaxBatchSize { get; set; } = 64;

        public TimeSpan MaxClientTimeout { get; set; } = TimeSpan.FromSeconds(60);

        ConcurrentDictionary<int, PartitionPublisher<TKey, TValue>> itemsByPartition = new ConcurrentDictionary<int, PartitionPublisher<TKey, TValue>>(); 

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
                return new PartitionPublisher<TKey, TValue>(this.executor, this.consumer, this.MaxBatchSize, this.MaxClientTimeout, this.logger);
            });

            partitionPublisher.Publish(consumeResult, cancellationToken);
        }
    }
}