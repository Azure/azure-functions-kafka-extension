// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
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
        private readonly ITriggeredFunctionExecutor executor;
        private readonly bool singleDispatch;
        private readonly ILogger logger;
        private readonly string brokerList;
        private readonly string topic;
        private readonly string consumerGroup;
        private readonly string eventHubConnectionString;
        private readonly string avroSchema;
        private readonly int maxBatchSize;
        private readonly ConcurrentDictionary<int, FunctionExecutorBase<TKey, TValue>> ownedPartitions = new ConcurrentDictionary<int, FunctionExecutorBase<TKey, TValue>>();
        private Consumer<TKey, TValue> consumer;
        private bool disposed;
        private CancellationToken cancellationToken;

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
        }

        public void Cancel()
        {
            this.SafeCloseConsumer();
        }        

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.cancellationToken = cancellationToken;

            var builder = new ConsumerBuilder<TKey, TValue>(this.GetConsumerConfiguration())
                .SetErrorHandler((_, e) => {
                    this.logger.LogError(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        foreach (var tp in e.Partitions)
                        {
                            this.EnsurePartitionExecutorExists(tp.Partition);
                        }

                        this.logger.LogInformation($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                        // possibly override the default partition assignment behavior:
                        // consumer.Assign(...) 
                    }
                    else
                    {
                        foreach (var tp in e.Partitions)
                        {
                            if (this.ownedPartitions.TryRemove(tp.Partition, out var partitionFunctionExecutor))
                            {
                                partitionFunctionExecutor.Dispose();
                            }
                        }

                        this.logger.LogInformation($"Revoked partitions: [{string.Join(", ", e.Partitions)}]");
                        // consumer.Unassign()
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
            this.consumer.Subscribe(this.topic);

            // Using a thread as opposed to a task since this will be long running
            // https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#avoid-using-taskrun-for-long-running-work-that-blocks-the-thread
            var thread = new Thread(ProcessSubscription)
            {
                IsBackground = true,
            };
            thread.Start();


            return Task.CompletedTask;
        }

        private FunctionExecutorBase<TKey, TValue> EnsurePartitionExecutorExists(int partition)
        {
            return this.ownedPartitions.GetOrAdd(partition, (p) =>
            {
                var functionExecutor = this.singleDispatch ?
                    (FunctionExecutorBase<TKey, TValue>)new SingleItemFunctionExecutor<TKey, TValue>(this.executor, this.consumer, this.cancellationToken, this.logger) :
                    new MultipleItemFunctionExecutor<TKey, TValue>(this.executor, this.consumer, this.cancellationToken, this.logger);

                return functionExecutor;
            });
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
            var messages = new List<ConsumeResult<TKey, TValue>>(this.maxBatchSize);

            try
            {
                var alreadyFlushedPartitions = new HashSet<int>();
                while (!this.cancellationToken.IsCancellationRequested)
                {
                    var batchStart = DateTime.UtcNow;
                    var availableTime = this.MaxBatchReleaseTime - (DateTime.UtcNow - batchStart);

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
                                    var partitionExecutor = this.EnsurePartitionExecutorExists(kafkaEventData.Partition);
                                    var currentSize = partitionExecutor.Add(kafkaEventData);
                                    if (currentSize == this.maxBatchSize)
                                    {
                                        partitionExecutor.Flush();
                                        alreadyFlushedPartitions.Add(kafkaEventData.Partition);
                                    }
                                }
                            }

                            availableTime = this.MaxBatchReleaseTime - (DateTime.UtcNow - batchStart);
                        }
                        catch (ConsumeException ex)
                        {
                            this.logger.LogError(ex, $"Consume error");
                        }
                    }

                    // flush all partitions
                    foreach (var kv in this.ownedPartitions)
                    {
                        // Do not flush a partition that was recently flushed
                        // It can lead to excessive function calls
                        if (!alreadyFlushedPartitions.Contains(kv.Key))
                        {
                            kv.Value.Flush();
                        }
                    }

                    alreadyFlushedPartitions.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                this.logger.LogInformation("Closing consumer.");
                this.SafeCloseConsumer();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            SafeCloseConsumer();

            return Task.CompletedTask;
        }

        private void SafeCloseConsumer()
        {
            if (this.consumer != null)
            {
                this.consumer.Unsubscribe();

                foreach (var kv in this.ownedPartitions)
                {
                    kv.Value.Dispose();
                }
                this.ownedPartitions.Clear();


                this.consumer.Dispose();
                this.consumer = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    SafeCloseConsumer();
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