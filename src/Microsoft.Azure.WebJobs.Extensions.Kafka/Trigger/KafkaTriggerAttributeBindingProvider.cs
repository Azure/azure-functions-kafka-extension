// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Trigger;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IConfiguration config;
        private readonly IConverterManager converterManager;
        private readonly INameResolver nameResolver;
        private readonly IOptions<KafkaOptions> options;
        private readonly ILogger logger;

        public KafkaTriggerAttributeBindingProvider(
            IConfiguration config,
            IOptions<KafkaOptions> options,
            IConverterManager converterManager,
            INameResolver nameResolver,
            ILoggerFactory loggerFactory)
        {
            this.config = config;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
            this.options = options;
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

            var consumerConfig = CreateConsumerConfiguration(attribute);

            var keyAndValueTypes = SerializationHelper.GetKeyAndValueTypes(attribute.AvroSchema, parameter.ParameterType, typeof(string));
            var valueDeserializer = SerializationHelper.ResolveValueDeserializer(keyAndValueTypes.ValueType, keyAndValueTypes.AvroSchema);            

            var binding = CreateBindingStrategyFor(keyAndValueTypes.KeyType ?? typeof(Ignore), keyAndValueTypes.ValueType, keyAndValueTypes.RequiresKey, valueDeserializer, parameter, consumerConfig);
            return Task.FromResult<ITriggerBinding>(new KafkaTriggerBindingWrapper(binding));
        }

        ITriggerBinding CreateBindingStrategyFor(Type keyType, Type valueType, bool requiresKey, object valueDeserializer, ParameterInfo parameterInfo, KafkaListenerConfiguration listenerConfiguration)
        {
            var genericCreateBindingStrategy = this.GetType().GetMethod(nameof(CreateBindingStrategy), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(keyType, valueType);
            return (ITriggerBinding)genericCreateBindingStrategy.Invoke(this, new object[] { parameterInfo, listenerConfiguration, requiresKey, valueDeserializer });
        }

        private ITriggerBinding CreateBindingStrategy<TKey, TValue>(ParameterInfo parameter, KafkaListenerConfiguration listenerConfiguration, bool requiresKey, IDeserializer<TValue> valueDeserializer)
        {
            // TODO: reuse connections if they match with others in same function app
            Task<IListener> listenerCreator(ListenerFactoryContext factoryContext, bool singleDispatch)
            {
                var listener = new KafkaListener<TKey, TValue>(
                    factoryContext.Executor,
                    singleDispatch,
                    this.options.Value,
                    listenerConfiguration,
                    requiresKey,
                    valueDeserializer,
                    this.logger,
                    factoryContext.Descriptor.Id);
                
                return Task.FromResult<IListener>(listener);
            }

            return BindingFactory.GetTriggerBinding(new KafkaTriggerBindingStrategy<TKey, TValue>(), parameter, new KafkaEventDataConvertManager(this.converterManager, this.logger), listenerCreator);
        }

        private KafkaListenerConfiguration CreateConsumerConfiguration(KafkaTriggerAttribute attribute)
        {
            var consumerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = this.config.ResolveSecureSetting(nameResolver, attribute.BrokerList),
                ConsumerGroup = this.config.ResolveSecureSetting(nameResolver, attribute.ConsumerGroup),
                Topic = this.config.ResolveSecureSetting(nameResolver, attribute.Topic),
                EventHubConnectionString = this.config.ResolveSecureSetting(nameResolver, attribute.EventHubConnectionString),
                LagThreshold = attribute.LagThreshold
            };

            if (attribute.AuthenticationMode != BrokerAuthenticationMode.NotSet || 
                attribute.Protocol != BrokerProtocol.NotSet)
            {
                consumerConfig.SaslPassword = this.config.ResolveSecureSetting(nameResolver, attribute.Password);
                consumerConfig.SaslUsername = this.config.ResolveSecureSetting(nameResolver, attribute.Username);
                consumerConfig.SslKeyLocation = GetValidFilePath(attribute.SslKeyLocation);
                consumerConfig.SslKeyPassword = this.config.ResolveSecureSetting(nameResolver, attribute.SslKeyPassword);
                consumerConfig.SslCertificateLocation = GetValidFilePath(attribute.SslCertificateLocation);
                consumerConfig.SslCaLocation = GetValidFilePath(attribute.SslCaLocation);

                if (attribute.AuthenticationMode != BrokerAuthenticationMode.NotSet)
                {
                    consumerConfig.SaslMechanism = (SaslMechanism)attribute.AuthenticationMode;
                }

                if (attribute.Protocol != BrokerProtocol.NotSet)
                {
                    consumerConfig.SecurityProtocol = (SecurityProtocol)attribute.Protocol;
                }
            }

            return consumerConfig;
        }

        private string GetValidFilePath(string location)
        {
            var resolvedLocation = this.config.ResolveSecureSetting(nameResolver, location);
            if (!AzureFunctionsFileHelper.TryGetValidFilePath(resolvedLocation, out var validPath))
            {
                throw new Exception($"{location} is not a valid file location");
            }
            return validPath;
        }
    }
}