// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
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
            Task<IListener> listenerCreator(ListenerFactoryContext factoryContext, bool singleDispatch)
            {
                var listener = Listeners.KafkaListenerFactory.CreateFor(attribute,
                    factoryContext.Executor,
                    singleDispatch,
                    options.Value,
                    resolvedBrokerList,
                    resolvedTopic,
                    resolvedConsumerGroup,
                    resolvedEventHubConnectionString, logger);

                return Task.FromResult(listener);
            }

            var binding = BindingFactory.GetTriggerBinding(new KafkaTriggerBindingStrategy(), context.Parameter, this.converterManager, listenerCreator);
            return Task.FromResult<ITriggerBinding>(binding);
        }
    }
}