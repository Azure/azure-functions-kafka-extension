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
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Channel<KafkaEventData[]> channel;
        private readonly List<KafkaEventData> currentBatch;
        private readonly ILogger logger;
        private SemaphoreSlim readerFinished = new SemaphoreSlim(0, 1);

        public FunctionExecutorBase(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.logger = logger;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.currentBatch = new List<KafkaEventData>();

            this.channel = Channel.CreateBounded<KafkaEventData[]>(new BoundedChannelOptions(ChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true,
            });

            Task.Run(async () =>
            {
                await this.ReaderAsync(this.channel.Reader, this.cancellationTokenSource.Token, this.logger);
                this.readerFinished.Release();
            });
        }

        protected abstract Task ReaderAsync(ChannelReader<KafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger);


        protected void Commit(IEnumerable<TopicPartitionOffset> topicPartitionOffsets)
        {
            try
            {

                this.consumer.Commit(topicPartitionOffsets, this.cancellationTokenSource.Token);

                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    foreach (var tpo in topicPartitionOffsets)
                    {
                        this.logger.LogInformation("Committed {topic} / {partition} / {offset}",
                           tpo.Topic,
                           tpo.Partition,
                           tpo.Offset);
                    }
                }
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


            while (!this.cancellationTokenSource.IsCancellationRequested)
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

        bool isClosed = false;
        public async Task<bool> CloseAsync(TimeSpan timeout)
        {
            if (this.isClosed)
            {
                return true;
            }

            this.cancellationTokenSource.Cancel();
            this.channel.Writer.Complete();

            if (await this.readerFinished.WaitAsync(timeout))
            {
                this.isClosed = true;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            this.CloseAsync(TimeSpan.Zero).GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
