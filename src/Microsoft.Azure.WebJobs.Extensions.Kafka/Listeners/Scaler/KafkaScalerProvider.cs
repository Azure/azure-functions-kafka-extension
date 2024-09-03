// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaScalerProvider : IScaleMonitorProvider, ITargetScalerProvider
    {
        private readonly KafkaObjectTopicScaler _scaleMonitor;
        private readonly KafkaObjectTargetScaler _targetScaler;


        public KafkaScalerProvider(IServiceProvider serviceProvider, TriggerMetadata triggerMetadata)
        {
            IConfiguration config = serviceProvider.GetService<IConfiguration>();
            var nameResolver = serviceProvider.GetService<INameResolver>();
            KafkaMetaData kafkaMetadata = JsonConvert.DeserializeObject<KafkaMetaData>(triggerMetadata.Metadata.ToString());
            kafkaMetadata.ResolveProperties(serviceProvider.GetService<INameResolver>());
            IOptions<KafkaOptions> options = serviceProvider.GetService<IOptions<KafkaOptions>>();
            string topicName = kafkaMetadata.Topic;
            string consumerGroup = kafkaMetadata.ConsumerGroup;
            long lagThreshold = kafkaMetadata.LagThreshold;
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Kafka"));
            var metricsProvider = new KafkaMetricsProvider<Object, Object>(topicName, new AdminClientConfig(GetAdminConfiguration(kafkaMetadata)), logger);

            _scaleMonitor = new KafkaObjectTopicScaler(topicName, consumerGroup, metricsProvider, lagThreshold, logger);
            _targetScaler = new KafkaObjectTargetScaler(topicName, consumerGroup, metricsProvider, lagThreshold, logger);
        }

        private ConsumerConfig GetAdminConfiguration(KafkaMetaData kafkaMetaData)
        {
            return new ConsumerConfig()
            {
                GroupId = kafkaMetaData.ConsumerGroup,
                BootstrapServers = kafkaMetaData.BrokerList,
                SecurityProtocol = kafkaMetaData.Protocol,
                SaslMechanism = kafkaMetaData.SaslMechanism,
                SaslUsername = kafkaMetaData.Username,
                SaslPassword = kafkaMetaData.Password,
            };
        }

        public IScaleMonitor GetMonitor()
        {
            return _scaleMonitor;
        }

        public ITargetScaler GetTargetScaler()
        {
            return _targetScaler;
        }

        internal class KafkaMetaData
        {
            [JsonProperty]
            public string Topic { get; set; }

            [JsonProperty]
            public string BrokerList { get; set; }

            [JsonProperty]
            public string ConsumerGroup { get; set; }

            [JsonProperty]
            public long LagThreshold { get; set; }

            [JsonProperty]
            public string Username { get; set; }

            [JsonProperty]
            public string Password { get; set; }

            [JsonProperty]
            public SecurityProtocol Protocol { get; set; }

            [JsonProperty]
            public SaslMechanism SaslMechanism { get; set; }

            public void ResolveProperties(INameResolver resolver)
            {
                if (resolver != null)
                {
                    Topic = resolver.ResolveWholeString(Topic);
                    ConsumerGroup = resolver.ResolveWholeString(ConsumerGroup);
                    BrokerList = resolver.ResolveWholeString(BrokerList);
                    Username = resolver.ResolveWholeString(Username);
                    Password = resolver.ResolveWholeString(Password);
                }
            }
        }
    }
}
