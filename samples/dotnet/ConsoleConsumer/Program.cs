using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Newtonsoft.Json;

namespace ConsoleConsumer
{
    class Program
    {
        public static void Main(string[] args)
        {
            ConsumerConfig conf;
            var local = true;
            if (local)
            {
                conf = new ConsumerConfig
                {
                    BootstrapServers = "broker:9092",
                    GroupId = "consoleApp",
                    StatisticsIntervalMs = 5000,
                    SessionTimeoutMs = 6000,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true,
                    // { "debug", "security,broker,protocol" }       //Uncomment for librdkafka debugging information
                };
            }
            else
            {
                conf = new ConsumerConfig
                {
                    BootstrapServers = "<event-hub-name>.servicebus.windows.net:9093",
                    StatisticsIntervalMs = 5000,
                    SessionTimeoutMs = 6000,
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnablePartitionEof = true,
                    SecurityProtocol = SecurityProtocol.SaslSsl,
                    SaslMechanism = SaslMechanism.Plain,
                    SaslUsername = "$ConnectionString",
                    SaslPassword = "Endpoint=sb://<event-hub-name>.servicebus.windows.net/;SharedAccessKeyName=xxx;SharedAccessKey=xxxx",
                    SslCaLocation = "./cacert.pem",
                    GroupId = "consoleApp",
                    BrokerVersionFallback = "1.0.0",
                   // { "debug", "security,broker,protocol" }       //Uncomment for librdkafka debugging information
                };
            }

            var cts = new CancellationTokenSource();
            // StartPageViewConsumer(conf, cts.Token);
            // StartPageViewRegionConsumer(conf, cts.Token);
            StartPageViewConsumerAsGeneric(conf, cts.Token);
            //StartPageViewRegionConsumerAsGeneric(conf, cts.Token);
            Console.ReadLine();
            cts.Cancel();

        }

        private static void StartPageViewRegionConsumer(ConsumerConfig conf, CancellationToken cts = default)
        {
            var builder = new ConsumerBuilder<Ignore, PageViewRegion>(conf)
                .SetErrorHandler((_, e) => {
                    Console.WriteLine(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        Console.WriteLine($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                        // possibly override the default partition assignment behavior:
                        // consumer.Assign(...) 
                    }
                    else
                    {
                        Console.WriteLine($"Revoked partitions: [{string.Join(", ", e.Partitions)}]");
                        // consumer.Unassign()
                    }
                });

            var avroSchema = PageViewRegion._SCHEMA;
            var schemaRegistry = new LocalSchemaRegistry(avroSchema.ToString());
            if (avroSchema != null)
            {
                // if (typeof(TKey) != typeof(Ignore))
                //     builder.SetKeyDeserializer(new MagicAvroDeserializer<TKey>(new AvroDeserializer<TKey>(schemaRegistry)));

                builder.SetValueDeserializer(new MagicAvroDeserializer<PageViewRegion>(new AvroDeserializer<PageViewRegion>(schemaRegistry)));
            }
            
            var consumer = builder.Build();
            consumer.Subscribe("PAGEVIEWS_REGIONS");

            const int commitPeriod = 5;

            Task.Run(() => 
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(cts);

                            if (consumeResult.IsPartitionEOF)
                            {
                                Console.WriteLine($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                                continue;
                            }

                            Console.WriteLine(JsonConvert.SerializeObject(consumeResult));

                            if (consumeResult.Offset % commitPeriod == 0)
                            {
                                // The Commit method sends a "commit offsets" request to the Kafka
                                // cluster and synchronously waits for the response. This is very
                                // slow compared to the rate at which the consumer is capable of
                                // consuming messages. A high performance application will typically
                                // commit offsets relatively infrequently and be designed handle
                                // duplicate messages in the event of failure.
                                try
                                {
                                    consumer.Commit(consumeResult);
                                }
                                catch (KafkaException e)
                                {
                                    Console.WriteLine($"Commit error: {e.Error.Reason}");
                                }
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Closing consumer.");
                }
            });
        }

        private static void StartPageViewConsumer(ConsumerConfig conf, CancellationToken cts = default)
        {
            var builder = new ConsumerBuilder<Ignore, PageViews>(conf)
                .SetErrorHandler((_, e) => {
                    Console.WriteLine(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        Console.WriteLine($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                        // possibly override the default partition assignment behavior:
                        // consumer.Assign(...) 
                    }
                    else
                    {
                        Console.WriteLine($"Revoked partitions: [{string.Join(", ", e.Partitions)}]");
                        // consumer.Unassign()
                    }
                });

            var avroSchema = PageViews._SCHEMA;
            var schemaRegistry = new LocalSchemaRegistry(avroSchema.ToString());
            if (avroSchema != null)
            {
                // if (typeof(TKey) != typeof(Ignore))
                //     builder.SetKeyDeserializer(new MagicAvroDeserializer<TKey>(new AvroDeserializer<TKey>(schemaRegistry)));

                builder.SetValueDeserializer(new MagicAvroDeserializer<PageViews>(new AvroDeserializer<PageViews>(schemaRegistry)));
            }
            
            var consumer = builder.Build();
            consumer.Subscribe("pageviews");

            const int commitPeriod = 5;

            Task.Run(() => 
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(cts);

                            if (consumeResult.IsPartitionEOF)
                            {
                                Console.WriteLine($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                                continue;
                            }

                            Console.WriteLine(JsonConvert.SerializeObject(consumeResult));

                            if (consumeResult.Offset % commitPeriod == 0)
                            {
                                // The Commit method sends a "commit offsets" request to the Kafka
                                // cluster and synchronously waits for the response. This is very
                                // slow compared to the rate at which the consumer is capable of
                                // consuming messages. A high performance application will typically
                                // commit offsets relatively infrequently and be designed handle
                                // duplicate messages in the event of failure.
                                try
                                {
                                    consumer.Commit(consumeResult);
                                }
                                catch (KafkaException e)
                                {
                                    Console.WriteLine($"Commit error: {e.Error.Reason}");
                                }
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Closing consumer.");
                }
            });
        }

        private static void StartPageViewConsumerAsGeneric(ConsumerConfig conf, CancellationToken cts = default)
        {
            var builder = new ConsumerBuilder<Ignore, GenericRecord>(conf)
                .SetErrorHandler((_, e) => {
                    Console.WriteLine(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        Console.WriteLine($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                        // possibly override the default partition assignment behavior:
                        // consumer.Assign(...) 
                    }
                    else
                    {
                        Console.WriteLine($"Revoked partitions: [{string.Join(", ", e.Partitions)}]");
                        // consumer.Unassign()
                    }
                });

            var avroSchema = PageViews._SCHEMA;
            var schemaRegistry = new LocalSchemaRegistry(avroSchema.ToString());
            if (avroSchema != null)
            {
                // if (typeof(TKey) != typeof(Ignore))
                //     builder.SetKeyDeserializer(new MagicAvroDeserializer<TKey>(new AvroDeserializer<TKey>(schemaRegistry)));


                builder.SetValueDeserializer(new MagicAvroDeserializer<GenericRecord>(new AvroDeserializer<GenericRecord>(schemaRegistry)));
            }
            
            var consumer = builder.Build();
            consumer.Subscribe("pageviews");

            const int commitPeriod = 5;

            Task.Run(() => 
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(cts);

                            if (consumeResult.IsPartitionEOF)
                            {
                                Console.WriteLine($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                                continue;
                            }

                            Console.WriteLine(JsonConvert.SerializeObject(consumeResult));

                            if (consumeResult.Offset % commitPeriod == 0)
                            {
                                // The Commit method sends a "commit offsets" request to the Kafka
                                // cluster and synchronously waits for the response. This is very
                                // slow compared to the rate at which the consumer is capable of
                                // consuming messages. A high performance application will typically
                                // commit offsets relatively infrequently and be designed handle
                                // duplicate messages in the event of failure.
                                try
                                {
                                    consumer.Commit(consumeResult);
                                }
                                catch (KafkaException e)
                                {
                                    Console.WriteLine($"Commit error: {e.Error.Reason}");
                                }
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Closing consumer.");
                }
            });
        }

        private static void StartPageViewRegionConsumerAsGeneric(ConsumerConfig conf, CancellationToken cts = default)
        {
            var builder = new ConsumerBuilder<Ignore, GenericRecord>(conf)
                .SetErrorHandler((_, e) => {
                    Console.WriteLine(e.Reason);
                })
                .SetRebalanceHandler((_, e) =>
                {
                    if (e.IsAssignment)
                    {
                        Console.WriteLine($"Assigned partitions: [{string.Join(", ", e.Partitions)}]");
                        // possibly override the default partition assignment behavior:
                        // consumer.Assign(...) 
                    }
                    else
                    {
                        Console.WriteLine($"Revoked partitions: [{string.Join(", ", e.Partitions)}]");
                        // consumer.Unassign()
                    }
                });

            var avroSchema = PageViews._SCHEMA;
            var schemaRegistry = new LocalSchemaRegistry(avroSchema.ToString());
            if (avroSchema != null)
            {
                // if (typeof(TKey) != typeof(Ignore))
                //     builder.SetKeyDeserializer(new MagicAvroDeserializer<TKey>(new AvroDeserializer<TKey>(schemaRegistry)));


                builder.SetValueDeserializer(new MagicAvroDeserializer<GenericRecord>(new AvroDeserializer<GenericRecord>(schemaRegistry)));
            }
            
            var consumer = builder.Build();
            consumer.Subscribe("PAGEVIEWS_REGIONS");

            const int commitPeriod = 5;

            Task.Run(() => 
            {
                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(cts);

                            if (consumeResult.IsPartitionEOF)
                            {
                                Console.WriteLine($"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");
                                continue;
                            }

                            Console.WriteLine(JsonConvert.SerializeObject(consumeResult));

                            if (consumeResult.Offset % commitPeriod == 0)
                            {
                                // The Commit method sends a "commit offsets" request to the Kafka
                                // cluster and synchronously waits for the response. This is very
                                // slow compared to the rate at which the consumer is capable of
                                // consuming messages. A high performance application will typically
                                // commit offsets relatively infrequently and be designed handle
                                // duplicate messages in the event of failure.
                                try
                                {
                                    consumer.Commit(consumeResult);
                                }
                                catch (KafkaException e)
                                {
                                    Console.WriteLine($"Commit error: {e.Error.Reason}");
                                }
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Closing consumer.");
                }
            });
        }
    }
}
