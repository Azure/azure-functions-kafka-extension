// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal partial class KafkaAttributeBindingProvider : IBindingProvider
    {
        private static readonly IKafkaProducerBindingProvider InnerProvider = new CompositeKafkaProducerBindingProvider(
            new AsyncCollectorArgumentBindingProvider(),
            new KafkaEventDataArgumentBindingProvider(),
            new StringArgumentBindingProvider(),
            new ByteArrayArgumentBindingProvider()
            );

        private readonly IKafkaProducerFactory kafkaProducerFactory;

        public KafkaAttributeBindingProvider(IKafkaProducerFactory kafkaProducerFactory)
        {
            this.kafkaProducerFactory = kafkaProducerFactory;
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
            var keyAndValueTypes = SerializationHelper.GetKeyAndValueTypes(attribute.AvroSchema, parameter.ParameterType);

            IBinding binding = new KafkaAttributeBinding(
                parameter.Name,
                attribute,
                this.kafkaProducerFactory,
                argumentBinding,
                keyAndValueTypes.KeyType,
                keyAndValueTypes.ValueType,
                keyAndValueTypes.AvroSchema);
            return Task.FromResult<IBinding>(binding);
        }
    }
}
