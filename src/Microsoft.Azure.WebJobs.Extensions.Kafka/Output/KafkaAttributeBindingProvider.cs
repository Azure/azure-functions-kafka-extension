// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Trigger;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal partial class KafkaAttributeBindingProvider : IBindingProvider
    {
        private static readonly IKafkaProducerBindingProvider InnerProvider = new CompositeKafkaProducerBindingProvider(
            new AsyncCollectorArgumentBindingProvider(),
            new KafkaEventDataArgumentBindingProvider(),
            new StringArgumentBindingProvider(),
            new ByteArrayArgumentBindingProvider(),
            new SerializableTypeArgumentBindingProvider()
            );

        private readonly IKafkaProducerFactory kafkaProducerFactory;
        private readonly IConfiguration config;
        private readonly INameResolver nameResolver;

        public KafkaAttributeBindingProvider(IConfiguration config, INameResolver nameResolver, IKafkaProducerFactory kafkaProducerFactory)
        {
            this.kafkaProducerFactory = kafkaProducerFactory;
            this.config = config;
            this.nameResolver = nameResolver;
        }

        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<KafkaAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<IBinding>(null);
            }


            IEnumerable<KeyValuePair<string, string>> schemaRegistryConfig = null;
            if (parameter.GetCustomAttributes().Any())
            {
                schemaRegistryConfig = parameter.GetCustomAttributes<SchemaRegistryConfigAttribute>(inherit: false)
                    .Select(configAttribute =>
                        new KeyValuePair<string, string>(configAttribute.Key, configAttribute.Value));
            }

            var argumentBinding = InnerProvider.TryCreate(parameter);
            var keyAndValueTypes = SerializationHelper.GetKeyAndValueTypes(attribute.AvroSchema, parameter.ParameterType, typeof(string));

            IBinding binding = new KafkaAttributeBinding(
                parameter.Name,
                attribute,
                this.kafkaProducerFactory,
                argumentBinding,
                keyAndValueTypes.KeyType,
                keyAndValueTypes.ValueType,
                keyAndValueTypes.AvroSchema,
                this.config,
                this.nameResolver,
                schemaRegistryConfig);
            return Task.FromResult<IBinding>(binding);
        }
    }
}
