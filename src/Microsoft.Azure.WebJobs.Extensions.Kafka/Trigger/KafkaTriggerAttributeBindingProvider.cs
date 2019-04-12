// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Confluent.Kafka;
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

            // TODO: reuse connections if they match with others in same function app
            Task<IListener> listenerCreator(ListenerFactoryContext factoryContext, bool singleDispatch)
            {
                var listener = KafkaListenerFactory.CreateFor(attribute,
                    parameter.ParameterType,
                    factoryContext.Executor,
                    singleDispatch,
                    this.options.Value,
                    consumerConfig,
                    logger);

                return Task.FromResult(listener);
            }

            #pragma warning disable CS0618 // Type or member is obsolete
            var binding = BindingFactory.GetTriggerBinding(new KafkaTriggerBindingStrategy(), context.Parameter, this.converterManager, listenerCreator);
            #pragma warning restore CS0618 // Type or member is obsolete

            return Task.FromResult<ITriggerBinding>(binding);
        }


        private KafkaListenerConfiguration CreateConsumerConfiguration(KafkaTriggerAttribute attribute)
        {
            var consumerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = this.config.ResolveSecureSetting(nameResolver, attribute.BrokerList),
                ConsumerGroup = this.nameResolver.ResolveWholeString(attribute.ConsumerGroup),
                Topic = this.nameResolver.ResolveWholeString(attribute.Topic),
                EventHubConnectionString = this.config.ResolveSecureSetting(nameResolver, attribute.EventHubConnectionString),
            };

            if (attribute.AuthenticationMode != BrokerAuthenticationMode.NotSet || 
                attribute.Protocol != BrokerProtocol.NotSet)
            {
                consumerConfig.SaslPassword = this.config.ResolveSecureSetting(nameResolver, attribute.Password);
                consumerConfig.SaslUsername = this.config.ResolveSecureSetting(nameResolver, attribute.Username);
                consumerConfig.SslKeyLocation = attribute.SslKeyLocation;

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
    }
}