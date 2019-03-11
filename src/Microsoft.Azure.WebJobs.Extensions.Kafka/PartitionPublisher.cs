using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class PartitionPublisher<TKey, TValue>
    {
        readonly List<KafkaEventData> items;
        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly ILogger logger;
        private DateTime lastPublishing = DateTime.UtcNow;

        public int MaxBatchSize { get; set; }

        public TimeSpan MaxClientTimeout { get; set; }

        object listSync = new object();

        public PartitionPublisher(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer,int maxBatchSize, TimeSpan maxClientTimeout, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.logger = logger;
            this.MaxBatchSize = maxBatchSize;
            this.MaxClientTimeout = maxClientTimeout;
            this.items = new List<KafkaEventData>(this.MaxBatchSize * 2);
        }

        internal void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            var consumeResultData = new ConsumeResultWrapper<TKey, TValue>(consumeResult);
            var eventData = new KafkaEventData(consumeResultData);
            KafkaEventData[] itemsToPublish = null;

            lock (this.listSync)
            {
                this.items.Add(eventData);

                // check if we need to publish
                if (this.items.Count >= this.MaxBatchSize || ElapsedSinceLastPublishing() > this.MaxClientTimeout)
                {
                    var amountOfItemsToPublish = Math.Min(this.items.Count, this.MaxBatchSize);
                    itemsToPublish = this.items.GetRange(0, amountOfItemsToPublish).ToArray();
                    this.items.RemoveRange(0, amountOfItemsToPublish);
                    this.lastPublishing = DateTime.UtcNow;
                }
            }

            if (itemsToPublish != null)
            {
                this.PublishRange(itemsToPublish, cancellationToken);
            }
        }

        private TimeSpan ElapsedSinceLastPublishing() => DateTime.UtcNow - this.lastPublishing;

        private void PublishRange(KafkaEventData[] itemsToPublish, CancellationToken cancellationToken)
        {
            var triggerInput = KafkaTriggerInput.New(itemsToPublish);
            var triggerData = new TriggeredFunctionData
            {
                TriggerValue = triggerInput,
            };

            executor.TryExecuteAsync(triggerData, cancellationToken)
                .ContinueWith(t => this.Commit(t, itemsToPublish.Last(), cancellationToken));

            this.logger.LogDebug("Published batch of {batchSize} items in topic: {topic}, partition: {partition}, offset: {startingOffset}-{endingOffset}",
                itemsToPublish.Length,
                itemsToPublish[0].Topic,
                itemsToPublish[0].Partition,
                itemsToPublish[0].Offset,
                itemsToPublish[itemsToPublish.Length - 1].Offset);
        }

        void Commit(Task previousTask, KafkaEventData lastItem, CancellationToken cancellationToken)
        {
            if (previousTask.IsCompleted && !previousTask.IsFaulted)
            {
                try
                {
                    this.consumer.Commit(new[] { new TopicPartitionOffset(lastItem.Topic, lastItem.Partition, lastItem.Offset) }, cancellationToken);
                    this.logger.LogDebug("Committted topic: {topic}, partition: {partition}, offset: {offset}",
                       lastItem.Topic,
                       lastItem.Partition,
                       lastItem.Offset);
                }
                catch (KafkaException e)
                {
                    this.logger.LogError(e, $"Commit error: {e.Error.Reason}");
                }
            }
        }
    }
}