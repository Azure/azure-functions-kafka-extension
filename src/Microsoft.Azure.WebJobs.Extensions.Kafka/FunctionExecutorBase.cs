// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{

    /// <summary>
    /// Executes the functions for an specific partition
    /// </summary>
    public abstract class FunctionExecutorBase<TKey, TValue> : IDisposable
    {
        const int ChannelWriteRetryInterval = 50;
        const int ChannelCapacity = 10;

        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly CancellationToken cancellationToken;
        private readonly Channel<KafkaEventData[]> channel;
        private readonly List<KafkaEventData> currentBatch;
        private readonly ILogger logger;

        public FunctionExecutorBase(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, CancellationToken cancellationToken, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.logger = logger;
            this.cancellationToken = cancellationToken;
            this.currentBatch = new List<KafkaEventData>();

            this.channel = Channel.CreateBounded<KafkaEventData[]>(new BoundedChannelOptions(ChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true,
            });

            Task.Run(() => this.ReaderAsync(this.channel.Reader, this.cancellationToken, this.logger));
        }

        protected abstract Task ReaderAsync(ChannelReader<KafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger);

        protected void Commit(KafkaEventData kafkaEventData)
        {
            try
            {

                this.consumer.Commit(new[] { new TopicPartitionOffset(kafkaEventData.Topic, kafkaEventData.Partition, kafkaEventData.Offset) });

                this.logger.LogInformation("Committed {topic} / {partition} / {offset}",
                       kafkaEventData.Topic,
                       kafkaEventData.Partition,
                       kafkaEventData.Offset);
            }
            catch (KafkaException e)
            {
                this.logger.LogError(e, $"Commit error: {e.Error.Reason}");
            }
        }

        /// <summary>
        /// Adds an item, returning the current pending amount
        /// </summary>
        internal int Add(KafkaEventData kafkaEventData)
        {
            this.currentBatch.Add(kafkaEventData);
            return this.currentBatch.Count;
        }

        /// <summary>
        /// Sends the items in queue to function execution pipeline
        /// </summary>
        internal void Flush()
        {
            if (this.currentBatch.Count == 0)
            {
                return;
            }

            var items = this.currentBatch.ToArray();
            this.currentBatch.Clear();

            var loggedWaitingForFunction = false;


            while (!this.cancellationToken.IsCancellationRequested)
            {
                if (channel.Writer.TryWrite(items))
                {
                    break;
                }

                if (!loggedWaitingForFunction)
                {
                    this.logger.LogInformation("Channel {topic} / {partition} / {offset} is full, waiting for the function execution to catch up",
                           items[0].Topic,
                           items[0].Partition,
                           items[0].Offset);

                    loggedWaitingForFunction = true;
                }

                Thread.Sleep(ChannelWriteRetryInterval);
            }
        }

        protected Task<FunctionResult> ExecuteFunctionAsync(TriggeredFunctionData triggerData, CancellationToken cancellationToken)
        {
            // TODO: add retry logic
            return this.executor.TryExecuteAsync(triggerData, cancellationToken);
        }

        public void Dispose()
        {
            this.channel.Writer.Complete();
            GC.SuppressFinalize(this);
        }
    }
}
