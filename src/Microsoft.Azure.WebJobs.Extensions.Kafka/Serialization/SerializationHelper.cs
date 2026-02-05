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
        internal static (object, object) ResolveDeserializers(GetKeyAndValueTypesResult keyAndValueTypes, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, string topic)
        {
            object valueDeserializer = null;
            object keyDeserializer = null;

            var valueType = keyAndValueTypes.ValueType;
            var keyType = keyAndValueTypes.KeyType;
            var specifiedValueAvroSchema = keyAndValueTypes.ValueAvroSchema;
            var specifiedKeyAvroSchema = keyAndValueTypes.KeyAvroSchema;

            // call helper if schema registry is specified and return avro deserializers
            if (schemaRegistryUrl != null)
            {
                return ResolveSchemaRegistryDeserializers(valueType, keyType, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword, topic);
            }

            // check for protobuf deserialization
            if (typeof(IMessage).IsAssignableFrom(valueType))
            {
                valueDeserializer = Activator.CreateInstance(typeof(ProtobufDeserializer<>).MakeGenericType(valueType));
            }
            if (typeof(IMessage).IsAssignableFrom(keyType))
            {
                keyDeserializer = Activator.CreateInstance(typeof(ProtobufDeserializer<>).MakeGenericType(keyType));
            }

            var isValueSpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(valueType);
            var isValueGenericRecord = typeof(GenericRecord).IsAssignableFrom(valueType);
            var isKeySpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(keyType);
            var isKeyGenericRecord = typeof(GenericRecord).IsAssignableFrom(keyType);
            // create schemas for specific records
            if (!string.IsNullOrWhiteSpace(specifiedValueAvroSchema) && isValueSpecificRecord)
            {
                specifiedValueAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
            }
            if (!string.IsNullOrWhiteSpace(specifiedKeyAvroSchema) && isKeySpecificRecord)
            {
                specifiedKeyAvroSchema = ((ISpecificRecord)Activator.CreateInstance(keyType)).Schema.ToString();
            }

            // check for avro deserialization
            if (specifiedValueAvroSchema != null || specifiedKeyAvroSchema != null)
            {
                // if value avro schema exists and value type is generic record, create avro deserializer
                if (!string.IsNullOrWhiteSpace(specifiedValueAvroSchema))
                {
                    // creates local schema registry to store only value schema since no schema registry url is specified
                    var valueSchemaRegistry = CreateSchemaRegistry(specifiedValueAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);
                    if (isValueGenericRecord || isValueSpecificRecord)
                    {
                        var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroValueDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                        var genericMethod = methodInfo.MakeGenericMethod(valueType);
                        valueDeserializer = genericMethod.Invoke(null, new object[] { valueSchemaRegistry });
                    }
                    else
                    {
                        throw new ArgumentException($"Value type {valueType.FullName} is not a valid data type when avro schema is provided. It must be a GenericRecord or ISpecificRecord.");
                    }
                }

                // if key avro schema exists and key type is a generic record, create avro deserializer
                if (!string.IsNullOrWhiteSpace(specifiedKeyAvroSchema))
                {
                    if (isKeyGenericRecord || isKeySpecificRecord)
                    {
                        // creates local schema registry to store only value schema since no schema registry url is specified
                        var keySchemaRegistry = CreateSchemaRegistry(specifiedKeyAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);
                        var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroKeyDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                        var genericMethod = methodInfo.MakeGenericMethod(keyType);
                        keyDeserializer = genericMethod.Invoke(null, new object[] { keySchemaRegistry });
                    }
                    else
                    {
                        throw new ArgumentException($"Key type {keyType.FullName} is not a valid data type when avro schema is provided. It must be a GenericRecord or ISpecificRecord.");
                    }
                }
            }

            return (valueDeserializer, keyDeserializer);
        }

        internal static (object, object) ResolveSchemaRegistryDeserializers(Type valueType, Type keyType, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, string topic)
        {
            object valueDeserializer = null;
            object keyDeserializer = null;

            var schemaRegistry = CreateSchemaRegistry(null, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);

            if (typeof(GenericRecord).IsAssignableFrom(valueType))
            {
                // retrieve schema and create deserializer
                var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroValueDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                var genericMethod = methodInfo.MakeGenericMethod(valueType);
                valueDeserializer = genericMethod.Invoke(null, new object[] { schemaRegistry });
            }
            else if (!typeof(ISpecificRecord).IsAssignableFrom(valueType) &&
                     !typeof(Google.Protobuf.IMessage).IsAssignableFrom(valueType))
            {
                // Fix for Issue #532: For Out-of-proc scenarios where valueType is string or byte[],
                // create a GenericRecord deserializer to properly handle Avro messages from Schema Registry.
                // The GenericRecord will be converted to JSON when sent to the out-of-proc worker.
                valueDeserializer = CreateAvroValueDeserializer<GenericRecord>(schemaRegistry);
            }

            // if keyType is genericRecord, create avro deserializer
            if (typeof(GenericRecord).IsAssignableFrom(keyType))
            {
                var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroKeyDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                var genericMethod = methodInfo.MakeGenericMethod(keyType);
                keyDeserializer = genericMethod.Invoke(null, new object[] { schemaRegistry });
            }

            return (valueDeserializer, keyDeserializer);
        }

        private static IDeserializer<TValue> CreateAvroValueDeserializer<TValue>(ISchemaRegistryClient schemaRegistry)
        {
            return new AvroDeserializer<TValue>(schemaRegistry).AsSyncOverAsync();
        }

        private static IDeserializer<TKey> CreateAvroKeyDeserializer<TKey>(ISchemaRegistryClient schemaRegistry)
        {
            return new AvroDeserializer<TKey>(schemaRegistry).AsSyncOverAsync();
        }

        internal static (object, object) ResolveSerializers(Type valueType, Type keyType, string specifiedValueAvroSchema, string specifiedKeyAvroSchema, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, string topic)
        {
            object keySerializer = null;
            object valueSerializer = null;

            // call helper if schema registry is specified and return avro serializers
            if (schemaRegistryUrl != null)
            {
                return ResolveSchemaRegistrySerializers(valueType, keyType, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword, topic);
            }

            // create serializers for protobuf
            if (typeof(IMessage).IsAssignableFrom(valueType))
            {
                valueSerializer = Activator.CreateInstance(typeof(ProtobufSerializer<>).MakeGenericType(valueType));
                if (typeof(IMessage).IsAssignableFrom(keyType))
                {
                    keySerializer = Activator.CreateInstance(typeof(ProtobufSerializer<>).MakeGenericType(keyType));
                }   
                return (valueSerializer, keySerializer);
            }

            var isValueSpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(valueType);
            var isValueGenericRecord = typeof(GenericRecord).IsAssignableFrom(valueType);
            var isKeySpecificRecord = typeof(ISpecificRecord).IsAssignableFrom(keyType);
            var isKeyGenericRecord = typeof(GenericRecord).IsAssignableFrom(keyType);
            // create schemas for specific records
            if (!string.IsNullOrWhiteSpace(specifiedValueAvroSchema) && isValueSpecificRecord)
            {
                specifiedValueAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
            }
            if (!string.IsNullOrWhiteSpace(specifiedKeyAvroSchema) && isKeySpecificRecord)
            {
                specifiedKeyAvroSchema = ((ISpecificRecord)Activator.CreateInstance(keyType)).Schema.ToString();
            }

            // check for avro serialization
            if (specifiedValueAvroSchema != null || specifiedKeyAvroSchema != null)
            {
                object serializer = null;

                // create serializers for avro - generic or specific records
                if (isValueGenericRecord || isValueSpecificRecord)
                {
                    // create schema registry client only for value schema
                    var valueSchemaRegistry = CreateSchemaRegistry(specifiedValueAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);
                    serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), valueSchemaRegistry, null /* config */);
                    valueSerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(valueType).Invoke(null, new object[] { serializer });
                }
                if (isKeyGenericRecord || isKeySpecificRecord)
                {
                    // create schema registry client only for key schema
                    var keySchemaRegistry = CreateSchemaRegistry(specifiedKeyAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);
                    serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(keyType), keySchemaRegistry, null /* config */);
                    keySerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(keyType).Invoke(null, new object[] { serializer });
                }
            }

            return (valueSerializer, keySerializer);
        }

        internal static (object, object) ResolveSchemaRegistrySerializers(Type valueType, Type keyType, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, string topic)
        {
            object valueSerializer = null;
            object keySerializer = null;

            var schemaRegistry = CreateSchemaRegistry(null, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword); 

            if (typeof(GenericRecord).IsAssignableFrom(valueType) || typeof(ISpecificRecord).IsAssignableFrom(valueType))
            {
                var serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
                valueSerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(valueType).Invoke(null, new object[] { serializer });
            }

            if (typeof(GenericRecord).IsAssignableFrom(keyType) || typeof(ISpecificRecord).IsAssignableFrom(keyType))
            {
                var serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(keyType), schemaRegistry, null /* config */);
                keySerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(keyType).Invoke(null, new object[] { serializer });
            }

            return (valueSerializer, keySerializer);
        }


        private static ISchemaRegistryClient CreateSchemaRegistry(string specifiedAvroSchema, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword)
        {
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
        internal static GetKeyAndValueTypesResult GetKeyAndValueTypes(string valueAvroSchemaFromAttribute, string keyAvroSchemaFromAttribute, Type parameterType, Type keyTypeFromAttribute)
        {
            string valueAvroSchema = null;
            string keyAvroSchema = null;
            var requiresKey = false;

            var valueType = parameterType;
            var keyType = keyTypeFromAttribute;

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
        /// Gets if the type can be serialized/deserialized.
        /// </summary>
        internal static bool IsDesSerType(Type type)
        {
            return type == typeof(GenericRecord) ||
                typeof(ISpecificRecord).IsAssignableFrom(type) ||
                typeof(IMessage).IsAssignableFrom(type);
        }
    }
}
