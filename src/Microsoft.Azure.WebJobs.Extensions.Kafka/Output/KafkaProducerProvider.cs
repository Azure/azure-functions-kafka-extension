// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
    /// Provider for <see cref="IKafkaProducer"/>
    /// Those matching the broker, key type and value type are shared
    /// </summary>
    public class KafkaProducerProvider : IKafkaProducerProvider
    {
        private readonly IConfiguration config;
        private readonly INameResolver nameResolver;
        private readonly ILoggerProvider loggerProvider;
        private ConcurrentDictionary<string, IKafkaProducer> producers = new ConcurrentDictionary<string, IKafkaProducer>();

        public KafkaProducerProvider(IConfiguration config, INameResolver nameResolver, ILoggerProvider loggerProvider)
        {
            this.config = config;
            this.nameResolver = nameResolver;
            this.loggerProvider = loggerProvider;
        }

        public IKafkaProducer Get(KafkaAttribute attribute)
        {
            var resolvedBrokerList = nameResolver.ResolveWholeString(attribute.BrokerList);
            var brokerListFromConfig = config.GetConnectionStringOrSetting(resolvedBrokerList);
            if (!string.IsNullOrEmpty(brokerListFromConfig))
            {
                resolvedBrokerList = brokerListFromConfig;
            }

            var keyTypeName = attribute.KeyType == null ? string.Empty : attribute.KeyType.AssemblyQualifiedName;
            var valueTypeName = attribute.ValueType == null ? string.Empty : attribute.ValueType.AssemblyQualifiedName;
            var producerKey = $"{resolvedBrokerList}:keyTypeName:valueTypeName";

            return producers.GetOrAdd(producerKey, (k) => Create(attribute, resolvedBrokerList));
        }

        private IKafkaProducer Create(KafkaAttribute attribute, string brokerList)
        {
            Type keyType = attribute.KeyType ?? typeof(Null);
            Type valueType = attribute.ValueType;
            string avroSchema = null;
            if (valueType == null)
            {
                if (!string.IsNullOrEmpty(attribute.AvroSchema))
                {
                    avroSchema = attribute.AvroSchema;
                    valueType = typeof(GenericRecord);
                }
                else
                {
                    valueType = typeof(string);
                }
            }
            else
            {
                if (typeof(ISpecificRecord).IsAssignableFrom(valueType))
                {
                    var specificRecord = (ISpecificRecord)Activator.CreateInstance(valueType);
                    avroSchema = specificRecord.Schema.ToString();
                }
            }

            return (IKafkaProducer)Activator.CreateInstance(
                typeof(KafkaProducer<,>).MakeGenericType(keyType, valueType),
                GetProducerConfig(attribute, brokerList),
                avroSchema,
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