// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Represents publish a single partition
    /// </summary>
    internal sealed class PartitionPublisher<TKey, TValue> : IDisposable
    {
        /// <summary>
        /// Defines the maximum amount of pending batches we can keep in memory before we pause the consumer
        /// </summary>
        const int MaxPendingBatches = 10;

        readonly List<KafkaEventData> items;
        readonly object listSync = new object();
        volatile int isPaused = 0;
        private readonly Channel<KafkaEventData[]> channel;
        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly CancellationToken cancellationToken;
        private readonly ILogger logger;
        private DateTime lastPublishing = DateTime.UtcNow;

        public int MaxBatchSize { get; set; }

        public TimeSpan MaxClientTimeout { get; set; }



        public PartitionPublisher(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, int maxBatchSize, TimeSpan maxClientTimeout, CancellationToken cancellationToken, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.logger = logger;
            this.MaxBatchSize = maxBatchSize;
            this.MaxClientTimeout = maxClientTimeout;
            this.cancellationToken = cancellationToken;
            this.items = new List<KafkaEventData>(this.MaxBatchSize * MaxPendingBatches);

            this.channel = Channel.CreateBounded<KafkaEventData[]>(new BoundedChannelOptions(MaxPendingBatches)
            {
                SingleReader = true,
                SingleWriter = true,
            });

            _= this.Reader();
        }

        private async Task Reader()
        {
            var reader = this.channel.Reader;
            while (await reader.WaitToReadAsync(this.cancellationToken))
            {
                while (reader.TryRead(out var itemsToPublish))
                {
                    try
                    {
                        // Try to publish them
                        var triggerInput = KafkaTriggerInput.New(itemsToPublish);
                        var triggerData = new TriggeredFunctionData
                        {
                            TriggerValue = triggerInput,
                        };


                        // TODO: add retry functionality
                        var functionResult = await executor.TryExecuteAsync(triggerData, this.cancellationToken);
                        if (functionResult.Succeeded)
                        {

                            this.logger.LogDebug("Published {batchSize} items in {topic} / {partition} / {startingOffset}-{endingOffset}",
                                itemsToPublish.Length,
                                itemsToPublish[0].Topic,
                                itemsToPublish[0].Partition,
                                itemsToPublish[0].Offset,
                                itemsToPublish[itemsToPublish.Length - 1].Offset);

                            this.Commit(itemsToPublish.Last());

                            // Function call succeeded, we can resume the partition if needed
                            this.Resume(itemsToPublish[0].Topic, itemsToPublish[0].Partition);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"Error in partition publisher reader");
                    }
                }
            }
        }

        /// <summary>
        /// Publishes an item according to the batch and bindings properties
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="consumeResult">Consume result.</param>
        internal void Publish(ConsumeResult<TKey, TValue> consumeResult)
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
                if (!this.channel.Writer.TryWrite(itemsToPublish))
                {
                    var referenceItem = itemsToPublish.First();
                    this.Pause(referenceItem.Topic, referenceItem.Partition);
                }
            }
        }

        /// <summary>
        /// Pauses the consumption of the current partition.
        /// This is caused by the triggerred function being too slow or throwing exceptions
        /// </summary>
        /// <param name="topic">Topic.</param>
        /// <param name="partition">Partition.</param>
        private void Pause(string topic, int partition)
        {
            var shouldPause = Interlocked.CompareExchange(ref this.isPaused, 1, 0) == 0;
            if (shouldPause)
            {
                this.consumer.Pause(new[] { new TopicPartition(topic, partition) });

                this.logger.LogInformation("Paused {topic} / {partition}",
                  topic,
                  partition);
            }
        }

        /// <summary>
        /// Resumes the consumption of the current partition
        /// </summary>
        /// <param name="topic">Topic.</param>
        /// <param name="partition">Partition.</param>
        private void Resume(string topic, int partition)
        {
            var shouldResume = Interlocked.CompareExchange(ref this.isPaused, 0, 1) == 1;
            if (shouldResume)
            {
                this.consumer.Resume(new[] { new TopicPartition(topic, partition) });

                this.logger.LogInformation("Resumed {topic} / {partition}",
                  topic,
                  partition);
            }
        }

        private TimeSpan ElapsedSinceLastPublishing() => DateTime.UtcNow - this.lastPublishing;

        void Commit(KafkaEventData lastItem)
        {
            try
            {
                this.consumer.Commit(new[] { new TopicPartitionOffset(lastItem.Topic, lastItem.Partition, lastItem.Offset) }, this.cancellationToken);
                this.logger.LogInformation("Committed {topic} / {partition} / {offset}",
                   lastItem.Topic,
                   lastItem.Partition,
                   lastItem.Offset);
            }
            catch (KafkaException e)
            {
                this.logger.LogError(e, $"Commit error: {e.Error.Reason}");
            }
        }

        public void Dispose()
        {
            this.channel.Writer.Complete();
            GC.SuppressFinalize(this);
        }
    }
}