// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using System;
using System.Reflection;
using System.Threading.Tasks;

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

            var argumentBinding = InnerProvider.TryCreate(parameter);
            var keyAndValueTypes = SerializationHelper.GetKeyAndValueTypes(attribute.AvroSchema, attribute.KeyAvroSchema, parameter.ParameterType, attribute.KeyDataType.GetDataType());

            IBinding binding = new KafkaAttributeBinding(
                parameter.Name,
                attribute,
                this.kafkaProducerFactory,
                argumentBinding,
                keyAndValueTypes.KeyType,
                keyAndValueTypes.ValueType,
                keyAndValueTypes.ValueAvroSchema,
                keyAndValueTypes.KeyAvroSchema,
                this.config,
                this.nameResolver);
            return Task.FromResult<IBinding>(binding);
        }
    }
}
