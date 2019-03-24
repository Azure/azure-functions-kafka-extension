// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;

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
        private readonly ILogger logger;
        private readonly string brokerList;
        private readonly string topic;
        private readonly string consumerGroup;
        private readonly string eventHubConnectionString;
        private readonly string avroSchema;
        private readonly int maxBatchSize;
        private FunctionExecutorBase<TKey, TValue> functionExecutor;
        private Consumer<TKey, TValue> consumer;
        private bool disposed;
        private CancellationTokenSource cancellationTokenSource;
        private SemaphoreSlim subscriberFinished;

        /// <summary>
        /// Gets or sets value indicating how long events will be read from Kafka before they are sent to the function
        /// If for a give partition it accumulates maxBatchSize elements the call to the function is started
        /// At the of the the batch period all partitions are flushed, restarting the process
        /// </summary>
        /// <value>The max batch release time.</value>
        public TimeSpan MaxBatchReleaseTime { get; private set; } = TimeSpan.FromSeconds(1);

        public KafkaListener(
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            ILogger logger,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            string avroSchema,
            int maxBatchSize)
        {
            this.executor = executor;
            this.singleDispatch = singleDispatch;
            this.logger = logger;
            this.brokerList = brokerList;
            this.topic = topic;
            this.consumerGroup = consumerGroup;
            this.eventHubConnectionString = eventHubConnectionString;
            this.avroSchema = avroSchema;
            this.maxBatchSize = maxBatchSize;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            this.SafeCloseConsumerAsync().GetAwaiter().GetResult();
        }        

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var builder = new ConsumerBuilder<TKey, TValue>(this.GetConsumerConfiguration())
                .SetErrorHandler((_, e) => {
                    this.logger.LogError(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        this.logger.LogInformation($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                    }
                    else
                    {
                        this.logger.LogInformation($"Revoked partitions: [{string.Join(", ", e.Partitions)}]");
                    }
                });


            if (!string.IsNullOrEmpty(this.avroSchema))
            {
                var schemaRegistry = new LocalSchemaRegistry(this.avroSchema);
                builder.SetValueDeserializer(new AvroDeserializer<TValue>(schemaRegistry));
            }
            else
            {
                if (typeof(Google.Protobuf.IMessage).IsAssignableFrom(typeof(TValue)))
                {
                    // protobuf: need to create using reflection due to generic requirements in ProtobufDeserializer
                    var serializer = (IDeserializer<TValue>)Activator.CreateInstance(typeof(ProtobufDeserializer<>).MakeGenericType(typeof(TValue)));
                    builder.SetValueDeserializer(serializer);
                }
            }

            this.consumer = builder.Build();

            this.functionExecutor = singleDispatch ?
                (FunctionExecutorBase<TKey, TValue>)new SingleItemFunctionExecutor<TKey, TValue>(this.executor, this.consumer, this.logger) :
                new MultipleItemFunctionExecutor<TKey, TValue>(this.executor, this.consumer, this.logger);

            this.consumer.Subscribe(this.topic);

            // Using a thread as opposed to a task since this will be long running
            // https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#avoid-using-taskrun-for-long-running-work-that-blocks-the-thread
            var thread = new Thread(ProcessSubscription)
            {
                IsBackground = true,
            };
            thread.Start(this.cancellationTokenSource.Token);

            return Task.CompletedTask;
        }

        private ConsumerConfig GetConsumerConfiguration()
        {
            ConsumerConfig conf;
            if (string.IsNullOrEmpty(this.eventHubConnectionString))
            {
                // Setup raw kafka configuration
                conf = new ConsumerConfig
                {
                    BootstrapServers = this.brokerList,
                    GroupId = consumerGroup,
                    StatisticsIntervalMs = 5000,
                    SessionTimeoutMs = 6000,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true,
                    // { "debug", "security,broker,protocol" }       //Uncomment for librdkafka debugging information
                };
            }
            else
            {
                var ehBrokerList = this.brokerList;
                if (!ehBrokerList.Contains(".servicebus.windows.net"))
                {
                    ehBrokerList = $"{ this.brokerList}.servicebus.windows.net:9093";
                }

                var consumerGroupToUse = string.IsNullOrEmpty(this.consumerGroup) ? "$Default" : this.consumerGroup;

                conf = new ConsumerConfig
                {
                    BootstrapServers = ehBrokerList,
                    StatisticsIntervalMs = 5000,
                    SessionTimeoutMs = 6000,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = "$ConnectionString",
                    SaslPassword = this.eventHubConnectionString,
                    SslCaLocation = "./cacert.pem",
                    GroupId = consumerGroupToUse,
                    BrokerVersionFallback = "1.0.0",
                    // { "debug", "security,broker,protocol" }       //Uncomment for librdkafka debugging information
                };
            }

            return conf;
        }

        private void ProcessSubscription(object parameter)
        {
            this.subscriberFinished = new SemaphoreSlim(0, 1);
            var cancellationToken = (CancellationToken)parameter;
            var messages = new List<ConsumeResult<TKey, TValue>>(this.maxBatchSize);

            try
            {
                var alreadyFlushedInCurrentExecution = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var batchStart = DateTime.UtcNow;
                    var availableTime = this.MaxBatchReleaseTime - (DateTime.UtcNow - batchStart);
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
                                    if (currentSize >= this.maxBatchSize)
                                    {
                                        this.functionExecutor.Flush();
                                        alreadyFlushedInCurrentExecution = true;
                                    }
                                }

                                availableTime = this.MaxBatchReleaseTime - (DateTime.UtcNow - batchStart);
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
                
            // Stop subscriber thread
            this.cancellationTokenSource.Cancel();

            // Stop function executor
            await this.functionExecutor?.CloseAsync(TimeSpan.FromMilliseconds(TimeToWaitForRunningProcessToEnd));

            // Wait for subscriber thread to end
            await this.subscriberFinished?.WaitAsync(TimeToWaitForRunningProcessToEnd);

            this.isClosed = true;

            this.consumer?.Unsubscribe();
            this.consumer?.Dispose();
            this.functionExecutor?.Dispose();
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