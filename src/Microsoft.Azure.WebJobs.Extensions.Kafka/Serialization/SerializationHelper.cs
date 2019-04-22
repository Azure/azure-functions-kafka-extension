// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
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
        /// Gets the type of the key and value.
        /// </summary>
        /// <param name="avroSchemaFromAttribute">Avro schema from attribute.</param>
        internal static (Type KeyType, Type ValueType, string AvroSchema) GetKeyAndValueTypes(string avroSchemaFromAttribute, Type parameterType, Type defaultKeyType)
        {
            string avroSchema = null;

            var valueType = parameterType;
            var keyType = defaultKeyType;

            while (valueType.HasElementType && valueType.GetElementType() != typeof(byte))
            {
                valueType = valueType.GetElementType();
            }            

            if (!valueType.IsPrimitive)
            {
                // todo: handle List<T>, arrays, etc
                if (valueType.IsGenericType)
                {
                    Type genericTypeDefinition = valueType.GetGenericTypeDefinition();

                    if (genericTypeDefinition == typeof(IAsyncCollector<>))
                    {
                        valueType = valueType.GetGenericArguments()[0];
                    }

                    if (valueType.IsGenericType)
                    {
                        var genericArgs = valueType.GetGenericArguments();
                        valueType = genericArgs.Last();
                        if (genericArgs.Length > 1)
                        {
                            keyType = genericArgs[0];
                        }
                    }
                }

                if (typeof(ISpecificRecord).IsAssignableFrom(valueType))
                {
                    var specificRecord = (ISpecificRecord)Activator.CreateInstance(valueType);
                    avroSchema = specificRecord.Schema.ToString();
                }
                else if (!string.IsNullOrEmpty(avroSchemaFromAttribute))
                {
                    avroSchema = avroSchemaFromAttribute;
                    valueType = typeof(Avro.Generic.GenericRecord);
                }
            }

            return (keyType, valueType, avroSchema);
        }
    }
}
