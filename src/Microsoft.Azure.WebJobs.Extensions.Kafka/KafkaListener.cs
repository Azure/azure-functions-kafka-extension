using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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

    internal class KafkaListener<TKey, TValue> : IListener
    {
        private ITriggeredFunctionExecutor executor;
        private bool singleDispatch;
        private readonly ILogger logger;
        private readonly string brokerList;
        private readonly string topic;
        private readonly string consumerGroup;
        private readonly string eventHubConnectionString;
        private readonly string avroSchema;
        private readonly int maxBatchSize;
        private Consumer<TKey, TValue> consumer;
        IKafkaMessagePublisher<TKey, TValue> publisher;
        private bool disposed;

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


            var builder = new ConsumerBuilder<TKey, TValue>(conf)
                .SetErrorHandler((_, e) => {
                    this.logger.LogError(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        this.logger.LogInformation($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                        // possibly override the default partition assignment behavior:
                        // consumer.Assign(...) 
                    }
                    else
                    {
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

            this.publisher = (this.singleDispatch) ?
                (IKafkaMessagePublisher<TKey, TValue>)new SingleKafkaMessagePublisher<TKey, TValue>(this.executor, this.consumer, this.logger) :
                new BatchKafkaMessagePublisher<TKey, TValue>(this.executor, this.consumer, this.maxBatchSize, this.logger);

            // Using a thread as opposed to a task since this will be long running
            // https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md#avoid-using-taskrun-for-long-running-work-that-blocks-the-thread
            var thread = new Thread(ProcessSubscription)
            {
                IsBackground = true,
            };
            thread.Start(cancellationToken);


            return Task.CompletedTask;
        }

        private void ProcessSubscription(object parameter)
        {
            var cancellationToken = (CancellationToken)parameter;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(cancellationToken);

                        if (consumeResult.IsPartitionEOF)
                        {
                            this.logger.LogInformation($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                            continue;
                        }

                        this.publisher.Publish(consumeResult, cancellationToken);

                    }
                    catch (ConsumeException ex)
                    {
                        this.logger.LogError(ex, $"Consume error");
                    }
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

        public void Dispose() => Dispose(true);
    }
}