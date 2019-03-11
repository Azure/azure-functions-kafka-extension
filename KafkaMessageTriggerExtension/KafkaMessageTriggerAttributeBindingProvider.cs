using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Confluent.Kafka;

namespace KafkaMessageTriggerExtension
{
    public class KafkaMessageTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly KafkaMessageExtensionConfig _extensionConfigProvider;

        public KafkaMessageTriggerAttributeBindingProvider(KafkaMessageExtensionConfig extensionConfigProvider)
        {
            _extensionConfigProvider = extensionConfigProvider;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<KafkaMessageTriggerAttribute>(false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            if (!IsSupportBindingType(parameter.ParameterType))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind KafkaMessageTriggerAttributeAttribute to type '{0}'.", parameter.ParameterType));
            }

            return Task.FromResult<ITriggerBinding>(new KafkaMessageTriggerBinding(context.Parameter, _extensionConfigProvider, context.Parameter.Member.Name));
        }

        public bool IsSupportBindingType(Type t)
        {
            return t == typeof(string) || t == typeof(ConsumeResult<Ignore, string>);
        }
    }
}