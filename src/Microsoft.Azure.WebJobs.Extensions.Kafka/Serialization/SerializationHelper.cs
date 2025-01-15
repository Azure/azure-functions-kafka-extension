// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        internal static object ResolveDeserializer(Type type, string specifiedAvroSchema, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword)
        {
            if (typeof(IMessage).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(typeof(ProtobufDeserializer<>).MakeGenericType(type));
            }

            var isSpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(type);
            if (!isSpecificRecord && !typeof(GenericRecord).IsAssignableFrom(type) && schemaRegistryUrl == null)
            {
                return null;
            }

            var schemaRegistry = CreateSchemaRegistry(type, specifiedAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword, isSpecificRecord);

            var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroValueDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
            var genericMethod = methodInfo.MakeGenericMethod(type);

            return genericMethod.Invoke(null, new object[] { schemaRegistry });
        }

        private static IDeserializer<TValue> CreateAvroValueDeserializer<TValue>(ISchemaRegistryClient schemaRegistry)
        {
            return new AvroDeserializer<TValue>(schemaRegistry).AsSyncOverAsync();
        }

        private static IDeserializer<TKey> CreateAvroKeyDeserializer<TKey>(ISchemaRegistryClient schemaRegistry)
        {
            return new AvroDeserializer<TKey>(schemaRegistry).AsSyncOverAsync();
        }

        internal static object ResolveValueSerializer(Type valueType, string specifiedAvroSchema, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword)
        {
            if (typeof(IMessage).IsAssignableFrom(valueType))
            {
                return Activator.CreateInstance(typeof(ProtobufSerializer<>).MakeGenericType(valueType));
            }

            var isSpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(valueType);
            if (!isSpecificRecord && !typeof(GenericRecord).IsAssignableFrom(valueType) && schemaRegistryUrl == null)
            {
                return null;
            }

            var schemaRegistry = CreateSchemaRegistry(valueType, specifiedAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword, isSpecificRecord);

            var serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
            return typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(valueType).Invoke(null, new object[] { serializer });
        }

        private static ISchemaRegistryClient CreateSchemaRegistry(Type valueType, string specifiedAvroSchema, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, bool isSpecificRecord)
        {
            if (string.IsNullOrWhiteSpace(specifiedAvroSchema) && isSpecificRecord)
            {
                specifiedAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
            }

            if (!string.IsNullOrWhiteSpace(specifiedAvroSchema))
            {
                return new LocalSchemaRegistry(specifiedAvroSchema);
            }
            if (schemaRegistryUrl != null)
            {
                var schemaRegistryConfig = new List<KeyValuePair<string, string>>();
                schemaRegistryConfig.Add(new KeyValuePair<string, string>("schema.registry.url", schemaRegistryUrl));
                if (schemaRegistryUsername != null && schemaRegistryPassword != null) {
                    var authString = schemaRegistryUsername + ":" + schemaRegistryPassword;
                    schemaRegistryConfig.Add(new KeyValuePair<string, string>("schema.registry.basic.auth.user.info", authString));
                }
                return new CachedSchemaRegistryClient(schemaRegistryConfig.ToArray());
            }
            throw new ArgumentNullException(nameof(specifiedAvroSchema), $@"parameter is required when creating an generic avro serializer");
        }

        internal class GetKeyAndValueTypesResult
        {
            public Type KeyType { get; set; }
            public bool RequiresKey { get; set; }
            public Type ValueType { get; set; }
            public string ValueAvroSchema { get; set; }
            public string KeyAvroSchema { get; set; }
        }

        /// <summary>
        /// Gets the type of the key and value.
        /// </summary>
        /// <param name="valueAvroSchemaFromAttribute">Avro schema of message value from attribute.</param>
        /// <param name="keyAvroSchemaFromAttribute">Avro schema of message key from attribute.</param>
        internal static GetKeyAndValueTypesResult GetKeyAndValueTypes(string valueAvroSchemaFromAttribute, string keyAvroSchemaFromAttribute, Type parameterType, Type defaultKeyType)
        {
            string valueAvroSchema = null;
            string keyAvroSchema = null;
            var requiresKey = false;

            var valueType = parameterType;
            var keyType = defaultKeyType;

            while (valueType.HasElementType && valueType.GetElementType() != typeof(byte))
            {
                valueType = valueType.GetElementType();
            }

            if (valueType.HasElementType && valueType.GetElementType() == typeof(byte))
            {
                keyType = typeof(byte[]);
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

                (valueType, valueAvroSchema) = GetTypeAndSchema(valueType, valueAvroSchemaFromAttribute);
            }

            (keyType, keyAvroSchema) = GetTypeAndSchema(keyType, keyAvroSchemaFromAttribute);

            return new GetKeyAndValueTypesResult
            {
                KeyType = keyType,
                ValueType = valueType,
                ValueAvroSchema = valueAvroSchema,
                KeyAvroSchema = keyAvroSchema,
                RequiresKey = requiresKey,
            };
        }

        private static (Type type, string avroSchema) GetTypeAndSchema(Type type, string avroSchemaFromAttributte)
        {
            string avroSchema = null;
            if (typeof(ISpecificRecord).IsAssignableFrom(type))
            {
                var specificRecord = (ISpecificRecord)Activator.CreateInstance(type);
                avroSchema = specificRecord.Schema.ToString();
            }
            else if (!string.IsNullOrEmpty(avroSchemaFromAttributte))
            {
                avroSchema = avroSchemaFromAttributte;
                type = typeof(Avro.Generic.GenericRecord);
            }
            return (type, avroSchema);
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
