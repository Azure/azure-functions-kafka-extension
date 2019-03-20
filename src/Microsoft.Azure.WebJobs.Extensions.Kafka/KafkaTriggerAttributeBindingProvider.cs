// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IConfiguration config;
        private readonly IConverterManager converterManager;
        private readonly INameResolver nameResolver;
        private readonly ILogger logger;

        public KafkaTriggerAttributeBindingProvider(
            IConfiguration config,
            ILoggerFactory loggerFactory,
            IConverterManager converterManager,
            INameResolver nameResolver)
        {
            this.config = config;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
            this.logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Kafka"));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<KafkaTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            var resolvedBrokerList = this.nameResolver.ResolveWholeString(attribute.BrokerList);
            var brokerListFromConfig = this.config.GetConnectionStringOrSetting(resolvedBrokerList);
            if (!string.IsNullOrEmpty(brokerListFromConfig))
            {
                resolvedBrokerList = brokerListFromConfig;
            }

            var resolvedConsumerGroup = this.nameResolver.ResolveWholeString(attribute.ConsumerGroup);
            var resolvedTopic = this.nameResolver.ResolveWholeString(attribute.Topic);

            string resolvedEventHubConnectionString = null;
            if (!string.IsNullOrWhiteSpace(attribute.EventHubConnectionString))
            {
                resolvedEventHubConnectionString = this.nameResolver.ResolveWholeString(attribute.EventHubConnectionString);
                var ehConnectionStringFromConfig = this.config.GetConnectionStringOrSetting(resolvedEventHubConnectionString);
                if (!string.IsNullOrEmpty(ehConnectionStringFromConfig))
                {
                    resolvedEventHubConnectionString = ehConnectionStringFromConfig;
                }
            }

            // TODO: reuse connections if they match with others in same function app
            Func<ListenerFactoryContext, bool, Task<IListener>> listenerCreator =
                (factoryContext, singleDispatch) =>
                {
                    Type keyType = attribute.KeyType ?? typeof(Ignore);
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

                    var listener = (IListener)Activator.CreateInstance(typeof(KafkaListener<,>).MakeGenericType(keyType, valueType), factoryContext.Executor, singleDispatch, this.logger, resolvedBrokerList, resolvedTopic, resolvedConsumerGroup, resolvedEventHubConnectionString, avroSchema, attribute.MaxBatchSize);
                    return Task.FromResult(listener);
                };

            var binding = BindingFactory.GetTriggerBinding(new KafkaTriggerBindingStrategy(), context.Parameter, this.converterManager, listenerCreator);
            return Task.FromResult<ITriggerBinding>(binding);
        }
    }
}