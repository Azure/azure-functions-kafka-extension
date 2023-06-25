// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class SerializationHelper
    {
        internal static object ResolveDeserializer(Type valueType, string specifiedAvroSchema)
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

                var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                var genericMethod = methodInfo.MakeGenericMethod(valueType);

                var schemaRegistry = new LocalSchemaRegistry(specifiedAvroSchema);
                return genericMethod.Invoke(null, new object[] { schemaRegistry });
            }
                       
            return null;
        }

        private static IDeserializer<TValue> CreateAvroDeserializer<TValue>(ISchemaRegistryClient schemaRegistry)
        {
            return new AvroDeserializer<TValue>(schemaRegistry).AsSyncOverAsync();
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
                var serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
                return typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(valueType).Invoke(null, new object[] { serializer });
            }

            return null;
        }

        internal class GetKeyAndValueTypesResult
        {
            public Type KeyType { get; set; }
            public bool RequiresKey { get; set; }
            public Type ValueType { get; set; }
            public string KeyAvroSchema { get; set; }
            public string ValueAvroSchema { get; set; }
        }

        /// <summary>
        /// Gets the type of the key and value.
        /// </summary>
        /// <param name="avroSchemaFromAttribute">Avro schema from attribute.</param>
        internal static GetKeyAndValueTypesResult GetKeyAndValueTypes(string avroSchemaFromAttribute, Type parameterType, Type defaultKeyType)
        {
            string avroSchema = null;
            string keyAvroSchema = null;
            var requiresKey = false;

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
                            requiresKey = true;
                            keyType = genericArgs[0];
                        }
                    }
                }

                if (typeof(ISpecificRecord).IsAssignableFrom(keyType))
                {
                    var specificRecord = (ISpecificRecord)Activator.CreateInstance(keyType);
                    keyAvroSchema = specificRecord.Schema.ToString();
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

            return new GetKeyAndValueTypesResult
            {
                KeyType = keyType,
                ValueType = valueType,
                ValueAvroSchema = avroSchema,
                RequiresKey = requiresKey,
                KeyAvroSchema = keyAvroSchema
            };
        }


        /// <summary>
        /// Gets if the type can be serialized/deserialized
        /// </summary>
        internal static bool IsDesSerType(Type type)
        {
            return type == typeof(GenericRecord) ||
                typeof(ISpecificRecord).IsAssignableFrom(type) ||
                typeof(IMessage).IsAssignableFrom(type);
        }
    }
}
