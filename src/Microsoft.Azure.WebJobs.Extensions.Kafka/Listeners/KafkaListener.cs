// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka listener.
    /// Connects a Kafka trigger function with a Kafka Consumer
    /// </summary>
    internal class KafkaListener<TKey, TValue> : IListener, IScaleMonitor<KafkaTriggerMetrics>
    {
        internal const string EventHubsBrokerVersionFallback = "1.0.0";
        internal const string EventHubsSaslUsername = "$ConnectionString";
        internal const string EventHubsBrokerListDns = ".servicebus.windows.net";
        internal const int EventHubsBrokerListPort = 9093;

        /// <summary>
        /// The time to wait for running process to end.
        /// </summary>
        const int TimeToWaitForRunningProcessToEnd = 10 * 1000;

        private readonly ITriggeredFunctionExecutor executor;
        private readonly bool singleDispatch;
        private readonly KafkaOptions options;
        private readonly KafkaListenerConfiguration listenerConfiguration;
        // Indicates if the consumer requires the Kafka element key
        private readonly bool requiresKey;
        private readonly ILogger logger;
        private FunctionExecutorBase<TKey, TValue> functionExecutor;
        private IConsumer<TKey, TValue> consumer;
        private bool disposed;
        private CancellationTokenSource cancellationTokenSource;
        private SemaphoreSlim subscriberFinished;
        private readonly String consumerGroup;
        private readonly String topicName;
        private readonly ScaleMonitorDescriptor scaleMonitorDescriptor;
        private readonly string functionId;
        //protected for the unit test
        protected KafkaTopicScaler<TKey, TValue> topicScaler;

        /// <summary>
        /// Gets the value deserializer
        /// </summary>
        /// <value>The value deserializer.</value>
        internal IDeserializer<TValue> ValueDeserializer { get; }

        public KafkaListener(
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            KafkaListenerConfiguration kafkaListenerConfiguration,
            bool requiresKey,
            IDeserializer<TValue> valueDeserializer,
            ILogger logger,
            FunctionDescriptor functionDescriptor)
        {
            this.ValueDeserializer = valueDeserializer;
            this.executor = executor;
            this.singleDispatch = singleDispatch;
            this.options = options;
            this.listenerConfiguration = kafkaListenerConfiguration;
            this.requiresKey = requiresKey;
            this.logger = logger;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.consumerGroup = string.IsNullOrEmpty(this.listenerConfiguration.ConsumerGroup) ? "$Default" : this.listenerConfiguration.ConsumerGroup;
            this.topicName = this.listenerConfiguration.Topic;
            this.functionId = functionDescriptor.Id;
            this.scaleMonitorDescriptor = new ScaleMonitorDescriptor($"{functionId}-kafka-{topicName}-{consumerGroup}".ToLower());
        }

        


        public void Cancel()
        {
            this.SafeCloseConsumerAsync().GetAwaiter().GetResult();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            AzureFunctionsFileHelper.InitializeLibrdKafka(this.logger);

            var builder = this.CreateConsumerBuilder(GetConsumerConfiguration());

            builder.SetErrorHandler((_, e) =>
            {
                logger.LogError(e.Reason);
            })
            .SetPartitionsAssignedHandler((_, e) =>
            {
                logger.LogInformation($"Assigned partitions: [{string.Join(", ", e)}]");
            })
            .SetPartitionsRevokedHandler((_, e) =>
            {
                logger.LogInformation($"Revoked partitions: [{string.Join(", ", e)}]");
            });

            if (ValueDeserializer != null)
            {
                builder.SetValueDeserializer(ValueDeserializer);
            }

            this.consumer = builder.Build();
            

            var commitStrategy = new AsyncCommitStrategy<TKey, TValue>(consumer, this.logger);

            functionExecutor = singleDispatch ?
                (FunctionExecutorBase<TKey, TValue>)new SingleItemFunctionExecutor<TKey, TValue>(executor, consumer, this.options.ExecutorChannelCapacity, this.options.ChannelFullRetryIntervalInMs, commitStrategy, logger) :
                new MultipleItemFunctionExecutor<TKey, TValue>(executor, consumer, this.options.ExecutorChannelCapacity, this.options.ChannelFullRetryIntervalInMs, commitStrategy, logger);

            consumer.Subscribe(this.listenerConfiguration.Topic);

            this.topicScaler = new KafkaTopicScaler<TKey, TValue>(this.listenerConfiguration.Topic, this.consumerGroup, scaleMonitorDescriptor, consumer, new AdminClientConfig(GetConsumerConfiguration()), this.logger);

            // Using a thread as opposed to a task since this will be long running
            var thread = new Thread(ProcessSubscription)
            {
                IsBackground = true,
            };
            thread.Start(cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates the ConsumerBuilder. Overriding in unit tests
        /// </summary>
        protected virtual ConsumerBuilder<TKey, TValue> CreateConsumerBuilder(ConsumerConfig config) => new ConsumerBuilder<TKey, TValue>(config);

        private ConsumerConfig GetConsumerConfiguration()
        {
            ConsumerConfig conf = new ConsumerConfig()
            {
                // enable auto-commit 
                EnableAutoCommit = true,

                // disable auto storing read offsets since we need to store them after calling the trigger function
                EnableAutoOffsetStore = false,

                // Interval in which commits stored in memory will be saved
                AutoCommitIntervalMs = this.options.AutoCommitIntervalMs,

                // start from earliest if no checkpoint has been committed
                AutoOffsetReset = AutoOffsetReset.Earliest,

                // Secure communication/authentication
                SaslMechanism = this.listenerConfiguration.SaslMechanism,
                SaslUsername = this.listenerConfiguration.SaslUsername,
                SaslPassword = this.listenerConfiguration.SaslPassword,
                SecurityProtocol = this.listenerConfiguration.SecurityProtocol,
                SslCaLocation = this.listenerConfiguration.SslCaLocation,
                SslCertificateLocation = this.listenerConfiguration.SslCertificateLocation,
                SslKeyLocation = this.listenerConfiguration.SslKeyLocation,
                SslKeyPassword = this.listenerConfiguration.SslKeyPassword,

                // Values from host configuration
                StatisticsIntervalMs = this.options.StatisticsIntervalMs,
                ReconnectBackoffMs = this.options.ReconnectBackoffMs,
                ReconnectBackoffMaxMs = this.options.ReconnectBackoffMaxMs,
                SessionTimeoutMs = this.options.SessionTimeoutMs,
                MaxPollIntervalMs = this.options.MaxPollIntervalMs,
                QueuedMinMessages = this.options.QueuedMinMessages,
                QueuedMaxMessagesKbytes = this.options.QueuedMaxMessagesKbytes,
                MaxPartitionFetchBytes = this.options.MaxPartitionFetchBytes,
                FetchMaxBytes = this.options.FetchMaxBytes,
            };

            if (string.IsNullOrEmpty(this.listenerConfiguration.EventHubConnectionString))
            {
                // Setup native kafka configuration
                conf.BootstrapServers = this.listenerConfiguration.BrokerList;
                conf.GroupId = this.listenerConfiguration.ConsumerGroup;
            }
            else
            {
                // Setup eventhubs kafka head configuration
                var ehBrokerList = this.listenerConfiguration.BrokerList;
                if (!ehBrokerList.Contains(EventHubsBrokerListDns))
                {
                    ehBrokerList = $"{this.listenerConfiguration.BrokerList}{EventHubsBrokerListDns}:{EventHubsBrokerListPort}";
                }

                var consumerGroupToUse = string.IsNullOrEmpty(this.listenerConfiguration.ConsumerGroup) ? "$Default" : this.listenerConfiguration.ConsumerGroup;
                conf.BootstrapServers = ehBrokerList;
                conf.SecurityProtocol = SecurityProtocol.SaslSsl;
                conf.SaslMechanism = SaslMechanism.Plain;
                conf.SaslUsername = EventHubsSaslUsername;
                conf.SaslPassword = this.listenerConfiguration.EventHubConnectionString;
                conf.SslCaLocation= this.EnsureValidEventHubsCertificateLocation(this.listenerConfiguration.SslCaLocation);
                conf.GroupId = consumerGroupToUse;
                conf.BrokerVersionFallback = EventHubsBrokerVersionFallback;
            }

            return conf;
        }

        string EnsureValidEventHubsCertificateLocation(string userProvidedLocation)
        {
            const string defaultEventhubsCertificateFilePath = "./cacert.pem";

            if (!string.IsNullOrWhiteSpace(userProvidedLocation))
            {
                if (!AzureFunctionsFileHelper.TryGetValidFilePath(userProvidedLocation, out var validatedUserProvidedLocation))
                {
                    throw new InvalidOperationException($"Could not find user provided event hubs certificate file '{userProvidedLocation}");
                }

                return validatedUserProvidedLocation;
            }

            if (!AzureFunctionsFileHelper.TryGetValidFilePath(defaultEventhubsCertificateFilePath, out var validatedCertificateFilePath))
            {
                throw new InvalidOperationException($"Could not find event hubs certificate file '{defaultEventhubsCertificateFilePath}'");
            }

            return validatedCertificateFilePath;
        }

        private void ProcessSubscription(object parameter)
        {
            this.subscriberFinished = new SemaphoreSlim(0, 1);
            var cancellationToken = (CancellationToken)parameter;
            var maxBatchSize = this.options.MaxBatchSize;
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
                                    var kafkaEventData = this.requiresKey ? 
                                        (IKafkaEventData)new KafkaEventData<TKey, TValue>(consumeResult) : 
                                        KafkaEventData<TValue>.CreateFrom(consumeResult);

                                    // add message to executor
                                    // if executor pending items is full, flush it
                                    var currentSize = this.functionExecutor.Add(kafkaEventData);
                                    if (currentSize >= maxBatchSize)
                                    {
                                        this.functionExecutor.Flush();
                                        alreadyFlushedInCurrentExecution = true;
                                    }
                                }

                                availableTime = maxBatchReleaseTime - (DateTime.UtcNow - batchStart);
                            }
                            else
                            {
                                // TODO: maybe slow down if there isn't much incoming data
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
                this.logger.LogInformation("Exiting {processName} for {topic}", nameof(ProcessSubscription), this.listenerConfiguration.Topic);
                this.subscriberFinished.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await SafeCloseConsumerAsync();
        }

        int isClosed = 0;

        private async Task SafeCloseConsumerAsync()
        {
            if (Interlocked.Exchange(ref isClosed, 1) == 1)
            {
                return;
            }

            try
            {
                // Stop subscriber thread
                this.cancellationTokenSource.Cancel();

                // Stop function executor                
                if (this.functionExecutor != null)
                {
                    await this.functionExecutor.CloseAsync(TimeSpan.FromMilliseconds(TimeToWaitForRunningProcessToEnd));
                }

                // Wait for subscriber thread to end                
                if (this.subscriberFinished != null)
                {
                    await this.subscriberFinished.WaitAsync(TimeToWaitForRunningProcessToEnd);
                }

                this.consumer?.Unsubscribe();
                this.consumer?.Dispose();
                this.functionExecutor?.Dispose();
                this.subscriberFinished?.Dispose();
                this.cancellationTokenSource.Dispose();                
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
                this.logger.LogInformation("Disposing Kafka Listener for {topic}", this.listenerConfiguration.Topic);

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
        }

        public ScaleMonitorDescriptor Descriptor => scaleMonitorDescriptor;

        public Task<KafkaTriggerMetrics> GetMetricsAsync() => topicScaler.GetMetricsAsync();

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await topicScaler.GetMetricsAsync();
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<KafkaTriggerMetrics> context) => topicScaler.GetScaleStatus(context);

        public ScaleStatus GetScaleStatus(ScaleStatusContext context) => topicScaler.GetScaleStatus(context);
    }
}