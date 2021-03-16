using Avro;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    public class KafkaLanguageEndToEndFixture : IAsyncLifetime
    {
        internal string Broker { get; set; } = "localhost:9092";

        internal ImmutableArray<string> Topics { get; } = ImmutableArray.Create<string>(new string[] { "java8result", "python38result" });

        private ConcurrentDictionary<string, IConsumer<string, string>> consumers = new ConcurrentDictionary<string, IConsumer<string, string>>();
        public HttpClient HttpClient { get; private set; }
        public Task DisposeAsync()
        {
            var exceptions = new List<KafkaException>();
            try
            {
                foreach (var consumer in consumers.Values)
                {
                    try
                    {
                        consumer.Close();
                    }
                    catch (KafkaException e)
                    {
                        exceptions.Add(e);
                    }
                }
                if (exceptions.Count > 0)
                {
                    throw new Exception($"{exceptions.Count} Consumer Close() failed ", exceptions.FirstOrDefault<KafkaException>());
                }
            }
            catch (KafkaException e)
            {
                throw e;
            }
            finally
            {
                this.HttpClient?.Dispose();
            }
            this.HttpClient?.Dispose();
            return Task.CompletedTask;
        }

        public IConsumer<string, string> ConsumerFactory(string key)
        {
            return consumers.GetOrAdd(key, (key) =>
             {
                 var config = new ConsumerConfig
                 {
                     BootstrapServers = this.Broker,
                     GroupId = key,
                     EnableAutoCommit = true
                 };

                 return new ConsumerBuilder<string, string>(config).SetLogHandler((k, v) => Console.WriteLine($"KafkaConsumer: {key} :{v?.Message}")).Build();
             });
        }

        public async Task InitializeAsync()
        {
            this.HttpClient = new HttpClient();
            var adminClientBuilder = new AdminClientBuilder(new AdminClientConfig()
            {
                BootstrapServers = this.Broker,

            });

            var adminClient = adminClientBuilder.Build();
            try
            {
                var createTopicOptions = new CreateTopicsOptions()
                {
                    OperationTimeout = TimeSpan.FromMinutes(2),
                    RequestTimeout = TimeSpan.FromMinutes(2),
                };

                await adminClient.CreateTopicsAsync(GetAllTopics(), createTopicOptions);

            }
            catch (CreateTopicsException createTopicsException)
            {
                if (!createTopicsException.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
                {
                    Console.WriteLine($"Error creation topics: {createTopicsException.ToString()}");
                    throw;
                }
            }
            catch (KafkaException ex)
            {
                if (!ex.Error.Reason.Equals("No topics to create", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw;
                }
            }
        }

        private IEnumerable<TopicSpecification> GetAllTopics()
        {
            return this.Topics.Select(p => new TopicSpecification() { Name = p, NumPartitions = 1, ReplicationFactor = 1 }).ToArray();
        }
    }
}
