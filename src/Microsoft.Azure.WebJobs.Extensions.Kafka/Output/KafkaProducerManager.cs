// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Creates <see cref="IKafkaProducer"/>
    /// Those matching the broker, key type and value type are shared
    /// </summary>
    public class KafkaProducerManager : IKafkaProducerManager
    {
        private readonly IConfiguration config;
        private readonly INameResolver nameResolver;
        ConcurrentDictionary<string, IKafkaProducer> producers = new ConcurrentDictionary<string, IKafkaProducer>();

        public KafkaProducerManager(IConfiguration config, INameResolver nameResolver)
        {
            this.config = config;
            this.nameResolver = nameResolver;
        }

        public IKafkaProducer Resolve(KafkaAttribute attribute)
        {
            var resolvedBrokerList = this.nameResolver.ResolveWholeString(attribute.BrokerList);
            var brokerListFromConfig = this.config.GetConnectionStringOrSetting(resolvedBrokerList);
            if (!string.IsNullOrEmpty(brokerListFromConfig))
            {
                resolvedBrokerList = brokerListFromConfig;
            }

            var keyTypeName = attribute.KeyType == null ? string.Empty : attribute.KeyType.AssemblyQualifiedName;
            var valueTypeName = attribute.ValueType == null ? string.Empty : attribute.ValueType.AssemblyQualifiedName;
            var producerKey = $"{resolvedBrokerList}:keyTypeName:valueTypeName";

            return producers.GetOrAdd(producerKey, (k) => this.Create(attribute, resolvedBrokerList));
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
                this.GetProducerConfig(brokerList),
                avroSchema);
        }

        private ProducerConfig GetProducerConfig(string brokerList)
        {
            return new ProducerConfig()
            {
                BootstrapServers = brokerList,
            };
        }
    }
}