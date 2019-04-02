// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka listener.
    /// Connects a Kafka trigger function with a Kafka Consumer
    /// </summary>
    internal class KafkaListener<TKey, TValue> : IListener
    {
        /// <summary>
        /// The time to wait for running process to end.
        /// </summary>
        const int TimeToWaitForRunningProcessToEnd = 10 * 1000;

        private readonly ITriggeredFunctionExecutor executor;
        private readonly bool singleDispatch;
        private readonly KafkaOptions options;
        private readonly ILogger logger;
        private readonly string brokerList;
        private readonly string topic;
        private readonly string consumerGroup;
        private readonly string eventHubConnectionString;
        private FunctionExecutorBase<TKey, TValue> functionExecutor;
        private IConsumer<TKey, TValue> consumer;
        private bool disposed;
        private CancellationTokenSource cancellationTokenSource;
        private SemaphoreSlim subscriberFinished;

        public KafkaListener(
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            ILogger logger)
        {
            this.executor = executor;
            this.singleDispatch = singleDispatch;
            this.options = options;
            this.logger = logger;
            this.brokerList = brokerList;
            this.topic = topic;
            this.consumerGroup = consumerGroup;
            this.eventHubConnectionString = eventHubConnectionString;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            this.SafeCloseConsumerAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Need to return a <see cref="IConsumer{TKey, TValue}"/> for unit tests.
        /// Unfortunately <see cref="ConsumerBuilder{TKey, TValue}"/> returns <see cref="Consumer{TKey, TValue}"/>
        /// </summary>
        protected virtual IConsumer<TKey, TValue> CreateConsumer(
            ConsumerConfig config,
            Action<Consumer<TKey, TValue>, Error> errorHandler,
            Action<IConsumer<TKey, TValue>, List<TopicPartition>> partitionsAssignedHandler,
            Action<IConsumer<TKey, TValue>, List<TopicPartitionOffset>> partitionsRevokedHandler,
            IAsyncDeserializer<TValue> asyncValueDeserializer = null,
            IDeserializer<TValue> valueDeserializer = null,
            IAsyncDeserializer<TKey> keyDeserializer = null
            )
        {
            var builder = new ConsumerBuilder<TKey, TValue>(config)
                .SetErrorHandler(errorHandler)
                .SetPartitionsAssignedHandler(partitionsAssignedHandler)
                .SetPartitionsRevokedHandler(partitionsRevokedHandler);

            if (keyDeserializer != null)
            {
                builder.SetKeyDeserializer(keyDeserializer);
            }

            if (asyncValueDeserializer != null)
            {
                builder.SetValueDeserializer(asyncValueDeserializer);
            }
            else if (valueDeserializer != null)
            {
                builder.SetValueDeserializer(valueDeserializer);
            }

            return builder.Build();
        }

        public virtual Task StartAsync(CancellationToken cancellationToken)
        {
            SetConsumerAndExecutor(null, null, null);

            return Task.CompletedTask;
        }

        protected void SetConsumerAndExecutor(IAsyncDeserializer<TValue> asyncValueDeserializer, IDeserializer<TValue> valueDeserializer, IAsyncDeserializer<TKey> keyDeserializer)
        {
            consumer = CreateConsumer(
                            config: GetConsumerConfiguration(),
                            errorHandler: (_, e) =>
                            {
                                logger.LogError(e.Reason);
                            },
                            partitionsAssignedHandler: (_, e) =>
                            {
                                logger.LogInformation($"Assigned partitions: [{string.Join(", ", e)}]");
                            },
                            partitionsRevokedHandler: (_, e) =>
                            {
                                logger.LogInformation($"Revoked partitions: [{string.Join(", ", e)}]");
                            },
                            asyncValueDeserializer: asyncValueDeserializer,
                            valueDeserializer: valueDeserializer,
                            keyDeserializer: keyDeserializer);

            functionExecutor = singleDispatch ?
                (FunctionExecutorBase<TKey, TValue>)new SingleItemFunctionExecutor<TKey, TValue>(executor, consumer, options.ExecutorChannelCapacity, options.ChannelFullRetryIntervalInMs, logger) :
                new MultipleItemFunctionExecutor<TKey, TValue>(executor, consumer, options.ExecutorChannelCapacity, options.ChannelFullRetryIntervalInMs, logger);

            consumer.Subscribe(topic);

            // Using a thread as opposed to a task since this will be long running
            // https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#avoid-using-taskrun-for-long-running-work-that-blocks-the-thread
            var thread = new Thread(ProcessSubscription)
            {
                IsBackground = true,
            };
            thread.Start(cancellationTokenSource.Token);
        }

        private ConsumerConfig GetConsumerConfiguration()
        {
            ConsumerConfig conf = new ConsumerConfig()
            {
                EnableAutoCommit = false, // we will commit manually
                AutoOffsetReset = AutoOffsetReset.Earliest, // start from earliest if no checkpoint has been committed
            };

            if (this.options.StatisticsIntervalMs.HasValue)
            {
                conf.StatisticsIntervalMs = this.options.StatisticsIntervalMs.Value;
            }

            if (this.options.ReconnectBackoffMs.HasValue)
            {
                conf.ReconnectBackoffMs = this.options.ReconnectBackoffMs.Value;
            }

            if (this.options.ReconnectBackoffMaxMs.HasValue)
            {
                conf.ReconnectBackoffMaxMs = this.options.ReconnectBackoffMaxMs.Value;
            }

            if (this.options.StatisticsIntervalMs.HasValue)
            {
                conf.StatisticsIntervalMs = this.options.StatisticsIntervalMs.Value;
            }

            if (this.options.SessionTimeoutMs.HasValue)
            {
                conf.SessionTimeoutMs = this.options.SessionTimeoutMs.Value;
            }

            if (this.options.MaxPollIntervalMs.HasValue)
            {
                conf.MaxPollIntervalMs = this.options.MaxPollIntervalMs.Value;
            }

            if (this.options.QueuedMinMessages.HasValue)
            {
                conf.QueuedMinMessages = this.options.QueuedMinMessages.Value;
            }

            if (this.options.QueuedMaxMessagesKbytes.HasValue)
            {
                conf.QueuedMaxMessagesKbytes = this.options.QueuedMaxMessagesKbytes.Value;
            }

            if (this.options.MaxPartitionFetchBytes.HasValue)
            {
                conf.MaxPartitionFetchBytes = this.options.MaxPartitionFetchBytes.Value;
            }

            if (this.options.FetchMaxBytes.HasValue)
            {
                conf.FetchMaxBytes = this.options.FetchMaxBytes.Value;
            }


            if (string.IsNullOrEmpty(this.eventHubConnectionString))
            {
                // Setup native kafka configuration
                conf.BootstrapServers = this.brokerList;
                conf.GroupId = consumerGroup;
            }
            else
            {
                // Setup eventhubs kafka head configuration
                var ehBrokerList = this.brokerList;
                if (!ehBrokerList.Contains(".servicebus.windows.net"))
                {
                    ehBrokerList = $"{ this.brokerList}.servicebus.windows.net:9093";
                }

                var consumerGroupToUse = string.IsNullOrEmpty(this.consumerGroup) ? "$Default" : this.consumerGroup;

                conf.BootstrapServers = ehBrokerList;
                conf.SecurityProtocol = SecurityProtocol.SaslSsl;
                conf.SaslMechanism = SaslMechanism.Plain;
                conf.SaslUsername = "$ConnectionString";
                conf.SaslPassword = this.eventHubConnectionString;
                conf.SslCaLocation = "./cacert.pem";
                conf.GroupId = consumerGroupToUse;
                conf.BrokerVersionFallback = "1.0.0";
            }

            return conf;
        }

        private void ProcessSubscription(object parameter)
        {
            this.subscriberFinished = new SemaphoreSlim(0, 1);
            var cancellationToken = (CancellationToken)parameter;
            var messages = new List<ConsumeResult<TKey, TValue>>(this.options.MaxBatchSize);

            var maxBatchReleaseTime = TimeSpan.FromSeconds(this.options.SubscriberIntervalInSeconds);

            try
            {
                var alreadyFlushedInCurrentExecution = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var batchStart = DateTime.UtcNow;
                    var availableTime = maxBatchReleaseTime - (DateTime.UtcNow - batchStart);
                    alreadyFlushedInCurrentExecution = false;

                    while (availableTime > TimeSpan.Zero)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(availableTime);

                            // If no message was consumed during the available time, returns null
                            if (consumeResult != null)
                            {
                                if (consumeResult.IsPartitionEOF)
                                {
                                    this.logger.LogInformation("Reached end of {topic} / {partition} / {offset}", consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);
                                }
                                else
                                {
                                    var kafkaEventData = new KafkaEventData(new ConsumeResultWrapper<TKey, TValue>(consumeResult));

                                    // add message to executor
                                    // if executor pending items is full, flush it
                                    var currentSize = this.functionExecutor.Add(kafkaEventData);
                                    if (currentSize >= this.options.MaxBatchSize)
                                    {
                                        this.functionExecutor.Flush();
                                        alreadyFlushedInCurrentExecution = true;
                                    }
                                }

                                availableTime = maxBatchReleaseTime - (DateTime.UtcNow - batchStart);
                            }
                            else
                            {
                                // TODO: maybe slow down if don't have much data incoming
                                break;
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            this.logger.LogError(ex, $"Consume error");
                        }
                    }

                    if (!alreadyFlushedInCurrentExecution)
                    {
                        this.functionExecutor.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error in Kafka subscriber");
            }
            finally
            {
                this.logger.LogInformation("Exiting {processName} for {topic}", nameof(ProcessSubscription), this.topic);
                this.subscriberFinished.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await SafeCloseConsumerAsync();
        }

        bool isClosed = false;
        private async Task SafeCloseConsumerAsync()
        {
            if (this.isClosed)
            {
                return;
            }

            try
            {
                this.isClosed = true;

                // Stop subscriber thread
                this.cancellationTokenSource.Cancel();

                // Stop function executor
                await this.functionExecutor?.CloseAsync(TimeSpan.FromMilliseconds(TimeToWaitForRunningProcessToEnd));

                // Wait for subscriber thread to end
                await this.subscriberFinished?.WaitAsync(TimeToWaitForRunningProcessToEnd);

                this.consumer?.Unsubscribe();
                this.consumer?.Dispose();
                this.functionExecutor?.Dispose();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to close Kafka listener");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                this.logger.LogInformation("Disposing Kafka Listener for {topic}", this.topic);

                if (disposing)
                {
                    this.SafeCloseConsumerAsync().GetAwaiter().GetResult();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}