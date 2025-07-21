// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    /// <summary>
    /// End to end tests fixture
    /// </summary>
    public class KafkaEndToEndTestFixture : IAsyncLifetime
    {
        internal string Broker { get; set; } = "localhost:9092";

        internal string EndToEndTestsGroupID = "endToEndTestsGroupID";

        internal TopicSpecification StringTopicWithOnePartition { get; } = new TopicSpecification() { Name = Constants.StringTopicWithOnePartitionName, NumPartitions = 1, ReplicationFactor = 1 };

        internal TopicSpecification StringTopicWithTenPartitions { get; } = new TopicSpecification() { Name = Constants.StringTopicWithTenPartitionsName, NumPartitions = 10, ReplicationFactor = 1 };

        internal TopicSpecification StringTopicWithLongKeyAndTenPartitions { get; } = new TopicSpecification() { Name = Constants.StringTopicWithLongKeyAndTenPartitionsName, NumPartitions = 10, ReplicationFactor = 1 };

        internal TopicSpecification MyAvroRecordTopic { get; } = new TopicSpecification() { Name = Constants.MyAvroRecordTopicName, NumPartitions = 10, ReplicationFactor = 1 };

        internal TopicSpecification MyKeyAvroRecordTopic { get; } = new TopicSpecification() { Name = Constants.MyKeyAvroRecordTopicName, NumPartitions = 10, ReplicationFactor = 1 };

        internal TopicSpecification MyProtobufTopic { get; } = new TopicSpecification() { Name = Constants.MyProtobufTopicName, NumPartitions = 10, ReplicationFactor = 1 };

        public KafkaEndToEndTestFixture()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.tests.json", optional: true)
                .AddJsonFile("appsettings.tests.local.json", optional: true)
                .Build();

            var brokerFromConfig = config["Values:LocalBroker"];
            if (!string.IsNullOrWhiteSpace(brokerFromConfig))
            {
                this.Broker = brokerFromConfig;
            }
        }


        /// <summary>
        /// Helper to get all topics used in the end to end tests
        /// </summary>
        /// <returns>The all topics.</returns>
        public IEnumerable<TopicSpecification> GetAllTopics()
        {
            foreach (var prop in this.GetType().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.PropertyType == typeof(TopicSpecification)))
            {
                yield return (TopicSpecification)prop.GetValue(this);
            }
        }

        public Task DisposeAsync() => Task.CompletedTask;

        /// <summary>
        /// Creates the required topics for end to end tests
        /// </summary>
        /// <returns>The async.</returns>
        public async Task InitializeAsync()
        {
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
    }
}
