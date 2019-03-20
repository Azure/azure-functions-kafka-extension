// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
    /// Publishes messages as they come, one at a time
    /// </summary>
    public class SingleKafkaMessagePublisher<TKey, TValue> : IKafkaMessagePublisher<TKey, TValue>
    {
        const int MaxBatchSize = 64;
        const int MaxPendingBatches = 10;
        const int CommitPeriod = 5;

        public TimeSpan MaxClientTimeout { get; set; } = TimeSpan.FromSeconds(60);

        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly CancellationToken cancellationToken;
        private readonly ILogger logger;
        private readonly List<KafkaEventData> items;
        private readonly Channel<KafkaEventData[]> channel;
        private readonly HashSet<int> ownedPartitions = new HashSet<int>();
        private readonly object listSync = new object();
        private volatile int isPaused = 0;
        private DateTime lastPublishing = DateTime.UtcNow;

        public SingleKafkaMessagePublisher(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, CancellationToken cancellationToken, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.cancellationToken = cancellationToken;
            this.logger = logger;

            this.items = new List<KafkaEventData>(MaxBatchSize * MaxPendingBatches);

            this.channel = Channel.CreateBounded<KafkaEventData[]>(new BoundedChannelOptions(MaxPendingBatches)
            {
                SingleReader = true,
                SingleWriter = true,
            });

            _ = this.Reader();
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
                        var partitionOffsets = new Dictionary<int, long>();
                        var calls = new List<Task<FunctionResult>>();
                        foreach (var item in itemsToPublish)
                        {
                            var triggerInput = KafkaTriggerInput.New(item);
                            var triggerData = new TriggeredFunctionData
                            {
                                TriggerValue = triggerInput,
                            };

                            calls.Add(executor.TryExecuteAsync(triggerData, this.cancellationToken));

                            // keep track of last offset by partition
                            partitionOffsets[item.Partition] = item.Offset;
                        }


                        await Task.WhenAll(calls);

                        var topic = itemsToPublish[0].Topic;
                        this.Commit(partitionOffsets.Select(kv => new TopicPartitionOffset(new TopicPartition(topic, kv.Key), kv.Value)));

                        // Function call succeeded, we can resume the partition if needed
                        this.Resume(topic);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, $"Error in partition publisher reader");
                    }
                }
            }
        }

        private void Resume(string topic)
        {
            var shouldResume = Interlocked.CompareExchange(ref this.isPaused, 0, 1) == 1;
            if (shouldResume)
            {
                TopicPartition[] topicPartitions = null;
                lock (this.ownedPartitions)
                {
                    topicPartitions = this.ownedPartitions.Select(x => new TopicPartition(topic, new Partition(x))).ToArray();
                }

                this.consumer.Resume(topicPartitions);

                foreach (var partition in topicPartitions)
                {
                    this.logger.LogInformation("Resume {topic} / {partition}",
                      topic,
                      partition);
                }
            }
        }

        private void Pause(string topic)
        {
            var shouldPause = Interlocked.CompareExchange(ref this.isPaused, 1, 0) == 0;
            if (shouldPause)
            {

                TopicPartition[] topicPartitions = null;
                lock (this.ownedPartitions)
                {
                    topicPartitions = this.ownedPartitions.Select(x => new TopicPartition(topic, new Partition(x))).ToArray();
                }

                this.consumer.Pause(topicPartitions);

                foreach (var partition in topicPartitions)
                {
                    this.logger.LogInformation("Paused {topic} / {partition}",
                      topic,
                      partition);
                }
            }
        }

        private void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
            try
            {
                this.consumer.Commit(offsets, this.cancellationToken);

                foreach (var offset in offsets)
                {
                    this.logger.LogInformation("Committed {topic} / {partition} / {offset}",
                       offset.Topic,
                       offset.Partition,
                       offset.Offset);
                }
            }
            catch (KafkaException e)
            {
                this.logger.LogError(e, $"Commit error: {e.Error.Reason}");
            }
        }

        /// <summary>
        /// Publishes an item according to the batch and bindings properties
        /// </summary>
        public void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            var consumeResultData = new ConsumeResultWrapper<TKey, TValue>(consumeResult);
            var eventData = new KafkaEventData(consumeResultData);
            KafkaEventData[] itemsToPublish = null;

            lock (this.listSync)
            {
                this.items.Add(eventData);

                // check if we need to publish
                if (this.items.Count >= MaxBatchSize || ElapsedSinceLastPublishing() > this.MaxClientTimeout)
                {
                    var amountOfItemsToPublish = Math.Min(this.items.Count, MaxBatchSize);
                    itemsToPublish = this.items.GetRange(0, amountOfItemsToPublish).ToArray();
                    this.items.RemoveRange(0, amountOfItemsToPublish);
                    this.lastPublishing = DateTime.UtcNow;
                }
            }

            if (itemsToPublish != null)
            {
                if (!this.channel.Writer.TryWrite(itemsToPublish))
                {
                    this.Pause(itemsToPublish[0].Topic);
                }
            }
        }

        private TimeSpan ElapsedSinceLastPublishing() => DateTime.UtcNow - this.lastPublishing;

        public void ClearPartitions(IList<TopicPartition> partitions)
        {
            lock (this.ownedPartitions)
            {
                foreach (var p in partitions)
                {
                    this.ownedPartitions.Remove(p.Partition);
                }
            }
        }

        public void AddPartitions(IList<TopicPartition> partitions)
        {
            lock (this.ownedPartitions)
            {
                foreach (var p in partitions)
                {
                    this.ownedPartitions.Add(p.Partition);
                }
            }
        }
    }
}