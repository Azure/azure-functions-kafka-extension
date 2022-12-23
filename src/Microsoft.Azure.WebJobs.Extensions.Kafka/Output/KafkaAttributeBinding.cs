// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{

    internal class KafkaAttributeBinding : IBinding
    {
        private readonly string parameterName;
        private readonly KafkaAttribute attribute;
        private readonly IKafkaProducerFactory kafkaProducerFactory;
        private readonly IArgumentBinding<KafkaProducerEntity> argumentBinding;
        private readonly Type keyType;
        private readonly Type valueType;
        private readonly string avroSchema;
        private readonly IConfiguration config;
        private readonly INameResolver nameResolver;
        private IEnumerable<KeyValuePair<string, string>> schemaRegistryConfig;

        public KafkaAttributeBinding(
            string parameterName, 
            KafkaAttribute attribute,
            IKafkaProducerFactory kafkaProducerFactory,
            IArgumentBinding<KafkaProducerEntity> argumentBinding,
            Type keyType,
            Type valueType,
            string avroSchema,
            IConfiguration config,
            INameResolver nameResolver,
            IEnumerable<KeyValuePair<string, string>> schemaRegistryConfig)
        {
            this.parameterName = parameterName;
            this.attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this.kafkaProducerFactory = kafkaProducerFactory ?? throw new ArgumentNullException(nameof(kafkaProducerFactory));
            this.argumentBinding = argumentBinding ?? throw new ArgumentNullException(nameof(argumentBinding));
            this.keyType = keyType;
            this.valueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            this.avroSchema = avroSchema;
            this.config = config;
            this.nameResolver = nameResolver;
            this.schemaRegistryConfig = schemaRegistryConfig;
        }

        public bool FromAttribute => true;

        public async Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var entity = new KafkaProducerEntity
            {
                KafkaProducerFactory = this.kafkaProducerFactory,
                KeyType = this.keyType ?? typeof(Null),
                ValueType = this.valueType,
                Topic = this.config.ResolveSecureSetting(this.nameResolver, this.attribute.Topic),
                Attribute = this.attribute,
                AvroSchema = this.avroSchema,
                SchemaRegistryConfig = this.schemaRegistryConfig,
            };

            return await BindAsync(entity, context);
        }

        public async Task<IValueProvider> BindAsync(BindingContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var entity = new KafkaProducerEntity
            {
                KafkaProducerFactory = this.kafkaProducerFactory,
                KeyType = this.keyType ?? typeof(Null),
                ValueType = this.valueType,
                Topic = this.config.ResolveSecureSetting(this.nameResolver, this.attribute.Topic),
                Attribute = this.attribute,
                AvroSchema = this.avroSchema,
                SchemaRegistryConfig = this.schemaRegistryConfig,
            };

            return await BindAsync(entity, context.ValueContext);
        }

        private Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
        {
            return argumentBinding.BindAsync(value, context);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor()
            {
                Name = parameterName,
                DisplayHints = new ParameterDisplayHints()
                {
                    Prompt = "Enter the Kafka event type KafkaEventData<TKey, TValue>",
                    DefaultValue = null
                },
            };
        }
    }
}