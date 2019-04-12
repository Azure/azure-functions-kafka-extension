// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Factory for <see cref="IKafkaProducer"/>
    /// Those matching the broker, key type and value type are shared
    /// </summary>
    public class KafkaProducerFactory : IKafkaProducerFactory
    {
        private readonly IConfiguration config;
        private readonly INameResolver nameResolver;
        private readonly ILoggerProvider loggerProvider;
        private readonly ConcurrentDictionary<string, IProducer<string, string>> baseProducers = new ConcurrentDictionary<string, IProducer<string, string>>();

        public KafkaProducerFactory(IConfiguration config, INameResolver nameResolver, ILoggerProvider loggerProvider)
        {
            this.config = config;
            this.nameResolver = nameResolver;
            this.loggerProvider = loggerProvider;
        }

        public IKafkaProducer Create(KafkaAttribute attribute)
        {
            var resolvedBrokerList = nameResolver.ResolveWholeString(attribute.BrokerList);
            var brokerListFromConfig = config.GetConnectionStringOrSetting(resolvedBrokerList);
            if (!string.IsNullOrEmpty(brokerListFromConfig))
            {
                resolvedBrokerList = brokerListFromConfig;
            }

            // Goal is to create as less producers as possible
            // We can group producers based on following criterias
            // - Broker List
            // - Configuration
            var producerConfig = this.GetProducerConfig(attribute, resolvedBrokerList);
            var producerKey = CreateKeyForConfig(producerConfig);

            var baseProducer = baseProducers.GetOrAdd(producerKey, (k) => CreateBaseProducer(producerConfig));
            return Create(baseProducer.Handle, attribute);
        }

        /// <summary>
        /// Creates a config key by concatenating all property key and values
        /// </summary>
        private string CreateKeyForConfig(ProducerConfig producerConfig)
        {
            var keyBuilder = new StringBuilder();

            foreach (var kv in producerConfig)
            {
                keyBuilder
                    .Append(kv.Key)
                    .Append('=')
                    .Append(kv.Value)
                    .Append(';');
            }

            return keyBuilder.ToString();
        }

        private IProducer<string, string> CreateBaseProducer(ProducerConfig producerConfig)
        {
            var builder = new ProducerBuilder<string, string>(producerConfig);
            return builder.Build();
        }

        private IKafkaProducer Create(Handle producerBaseHandle, KafkaAttribute attribute)
        {
            var valueType = SerializationHelper.GetValueType(attribute.ValueType, attribute.AvroSchema, null, out var avroSchema);
            var keyType = attribute.KeyType ?? typeof(Null);

            var valueSerializer = SerializationHelper.ResolveValueSerializer(valueType, attribute.AvroSchema);

            return (IKafkaProducer)Activator.CreateInstance(
                typeof(KafkaProducer<,>).MakeGenericType(keyType, valueType),
                producerBaseHandle,
                valueSerializer,
                loggerProvider.CreateLogger(LogCategories.CreateTriggerCategory("Kafka")));
        }

        private ProducerConfig GetProducerConfig(KafkaAttribute attribute, string brokerList) => new ProducerConfig
        {
            BootstrapServers = brokerList,
            BatchNumMessages = attribute.BatchSize,
            EnableIdempotence = attribute.EnableIdempotence,
            MessageSendMaxRetries = attribute.MaxRetries,
            MessageTimeoutMs = attribute.MessageTimeoutMs,
            RequestTimeoutMs = attribute.RequestTimeoutMs,
        };
    }
}