// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{

    /// <summary>
    /// Executes the functions for an specific partition.
    /// </summary>
    public abstract class FunctionExecutorBase<TKey, TValue> : IDisposable
    {
        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly int channelFullRetryIntervalInMs;
        private readonly ICommitStrategy<TKey, TValue> commitStrategy;
        private readonly CancellationTokenSource functionExecutionCancellationTokenSource;
        private readonly Channel<IKafkaEventData[]> channel;
        private readonly List<IKafkaEventData> currentBatch;
        private readonly ILogger logger;
        private readonly IDrainModeManager drainModeManager;
        private SemaphoreSlim readerFinished = new SemaphoreSlim(0, 1);

        internal FunctionExecutorBase(
            ITriggeredFunctionExecutor executor,
            IConsumer<TKey, TValue> consumer,
            int channelCapacity,
            int channelFullRetryIntervalInMs,
            ICommitStrategy<TKey, TValue> commitStrategy,
            ILogger logger,
            IDrainModeManager drainModeManager)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.channelFullRetryIntervalInMs = channelFullRetryIntervalInMs;
            this.commitStrategy = commitStrategy;
            this.logger = logger;
            this.functionExecutionCancellationTokenSource = new CancellationTokenSource();
            this.currentBatch = new List<IKafkaEventData>();
            this.drainModeManager = drainModeManager;

            this.channel = Channel.CreateBounded<IKafkaEventData[]>(new BoundedChannelOptions(channelCapacity)
            {
                SingleReader = true,
                SingleWriter = true,
            });

            Task.Run(async () =>
            {
                try
                {
                    await this.ReaderAsync(this.channel.Reader, this.functionExecutionCancellationTokenSource.Token, this.logger);
                }
                catch (Exception ex)
                {
                    // Channel reader will throw OperationCanceledException if cancellation token is cancelled during a call
                    if (!(ex is OperationCanceledException))
                    {
                        this.logger.LogError(ex, $"Function executor error while processing channel");
                    }
                }
                finally
                {
                    this.readerFinished.Release();
                }
            });
        }

        /// <summary>
        /// Channel reader, executing the function once data is available in channel.
        /// </summary>
        /// <param name="reader">The channel reader.</param>
        /// <param name="cancellationToken">Cancellation token indicating the host is shutting down.</param>
        /// <param name="logger">Logger.</param>
        protected abstract Task ReaderAsync(ChannelReader<IKafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger);


        protected void Commit(IEnumerable<TopicPartitionOffset> topicPartitionOffsets)
        {
            try
            {
                this.commitStrategy.Commit(topicPartitionOffsets);
            }
            catch (KafkaException e)
            {
                this.logger.LogError(e, $"Commit error: {e.Error.Reason}");
            }
        }

        /// <summary>
        /// Adds an item, returning the current pending amount.
        /// </summary>
        internal int Add(IKafkaEventData kafkaEventData)
        {
            this.currentBatch.Add(kafkaEventData);
            return this.currentBatch.Count;
        }

        /// <summary>
        /// Sends the items in queue to function execution pipeline.
        /// </summary>
        internal void Flush(CancellationToken cancellationToken)
        {
            if (this.currentBatch.Count == 0)
            {
                return;
            }

            var items = this.currentBatch.ToArray();
            this.currentBatch.Clear();

            var loggedWaitingForFunction = false;


            while (!cancellationToken.IsCancellationRequested)
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

                Thread.Sleep(this.channelFullRetryIntervalInMs);
            }
        }

        protected Task<FunctionResult> ExecuteFunctionAsync(TriggeredFunctionData triggerData, CancellationToken cancellationToken)
        {
            // TODO: add retry logic
            return this.executor.TryExecuteAsync(triggerData, cancellationToken);
        }

        bool isClosed = false;
        public async Task<bool> CloseAsync()
        {
            if (this.isClosed)
            {
                return true;
            }

            try
            {

                if (!drainModeManager.IsDrainModeEnabled)
                {
                    functionExecutionCancellationTokenSource.Cancel();
                }

                this.channel.Writer.Complete();

                if (await this.readerFinished.WaitAsync(TimeSpan.FromSeconds(300)))
                {
                    this.isClosed = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to close Kafka trigger executor");
            }

            return false;
        }

        public void Dispose()
        {
            this.CloseAsync().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
