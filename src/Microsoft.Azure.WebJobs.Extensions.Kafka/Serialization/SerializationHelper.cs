// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class SerializationHelper
    {
        internal static object ResolveValueDeserializer(Type valueType, string specifiedAvroSchema)
        {
            if (typeof(Google.Protobuf.IMessage).IsAssignableFrom(valueType))
            {
                return Activator.CreateInstance(typeof(ProtobufDeserializer<>).MakeGenericType(valueType));
            }

            var isSpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(valueType);
            if (isSpecificRecord || typeof(GenericRecord).IsAssignableFrom(valueType))
            {
                if (string.IsNullOrWhiteSpace(specifiedAvroSchema))
                {
                    if (!isSpecificRecord)
                    {
                        throw new ArgumentNullException(nameof(specifiedAvroSchema), $@"parameter is required when creating an generic avro deserializer");
                    }

                    specifiedAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
                }

                var schemaRegistry = new LocalSchemaRegistry(specifiedAvroSchema);
                return Activator.CreateInstance(typeof(AvroDeserializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
            }
                       
            return null;
        }

        internal static object ResolveValueSerializer(Type valueType, string specifiedAvroSchema)
        {
            if (typeof(Google.Protobuf.IMessage).IsAssignableFrom(valueType))
            {
                return Activator.CreateInstance(typeof(ProtobufSerializer<>).MakeGenericType(valueType));
            }

            var isSpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(valueType);
            if (isSpecificRecord || typeof(GenericRecord).IsAssignableFrom(valueType))
            {
                if (string.IsNullOrWhiteSpace(specifiedAvroSchema))
                {
                    if (!isSpecificRecord)
                    {
                        throw new ArgumentNullException(nameof(specifiedAvroSchema), $@"parameter is required when creating an generic avro serializer");
                    }

                    specifiedAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
                }

                var schemaRegistry = new LocalSchemaRegistry(specifiedAvroSchema);
                return Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
            }

            return null;
        }

        /// <summary>
        /// Gets the type of the value.
        /// </summary>
        /// <returns>The value type.</returns>
        /// <param name="typeFromAttribute">Type from attribute (KafkaAttribute or KafkaTriggerAttribute).</param>
        /// <param name="avroSchemaFromAttribute">Avro schema from attribute.</param>
        internal static Type GetValueType(Type typeFromAttribute, string avroSchemaFromAttribute, Type parameterType,  out string avroSchema)
        {
            avroSchema = null;

            var valueType = typeFromAttribute;
            if (valueType == null)
            {
                if (!string.IsNullOrEmpty(avroSchemaFromAttribute))
                {
                    avroSchema = avroSchemaFromAttribute;
                    return typeof(Avro.Generic.GenericRecord);
                }
                else if (parameterType == typeof(string))
                {
                    return typeof(string);
                }
                else
                {
                    return typeof(byte[]);
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

            return valueType;
        }
    }
}
