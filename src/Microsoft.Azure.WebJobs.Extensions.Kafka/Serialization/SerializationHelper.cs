// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
                return ResolveSchemaRegistryDeserializers(valueType, keyType, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword, topic).GetAwaiter().GetResult();
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
            if (string.IsNullOrWhiteSpace(specifiedValueAvroSchema) && isValueSpecificRecord)
            {
                specifiedValueAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
            }
            if (string.IsNullOrWhiteSpace(specifiedKeyAvroSchema) && isKeySpecificRecord)
            {
                specifiedKeyAvroSchema = ((ISpecificRecord)Activator.CreateInstance(keyType)).Schema.ToString();
            }

            // check for avro deserialization
            if (specifiedValueAvroSchema != null || specifiedKeyAvroSchema != null)
            {
                // creates local schema registry if no schema registry url is specified
                var schemaRegistry = CreateSchemaRegistry(specifiedValueAvroSchema, specifiedKeyAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);

                // if value avro schema exists, create avro deserializer
                if (!string.IsNullOrWhiteSpace(specifiedValueAvroSchema))
                {
                    var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroValueDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                    var genericMethod = methodInfo.MakeGenericMethod(valueType);
                    valueDeserializer = genericMethod.Invoke(null, new object[] { schemaRegistry });
                }

                // if key avro schema exists, create avro deserializer
                if (!string.IsNullOrWhiteSpace(specifiedKeyAvroSchema))
                {
                    var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroKeyDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                    var genericMethod = methodInfo.MakeGenericMethod(keyType);
                }
            }

            return (valueDeserializer, keyDeserializer);
        }

        internal static async Task<(object, object)> ResolveSchemaRegistryDeserializers(Type valueType, Type keyType, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, string topic)
        {
            object valueDeserializer = null;
            object keyDeserializer = null;

            var schemaRegistry = CreateSchemaRegistry(null, null, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);

            // check list of subjects for value and key schemas
            try
            {
                // make sure that valueType is genericRecord
                if (typeof(GenericRecord).IsAssignableFrom(valueType))
                {
                    var valueSchema = await schemaRegistry.GetLatestSchemaAsync(schemaRegistry.ConstructValueSubjectName(topic));
                    // retrieve schema and create deserializer
                    var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroKeyDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                    var genericMethod = methodInfo.MakeGenericMethod(valueType);
                    valueDeserializer = genericMethod.Invoke(null, new object[] { schemaRegistry });
                }
            }
            catch (SchemaRegistryException ex)
            {
                // if exception raises, value schema does not exist
                throw new TargetException($"Error finding avro schema for value in the schema registry via url. Exception: {ex.Message}");
            }

            try
            {
                // make sure that valueType is genericRecord
                if (typeof(GenericRecord).IsAssignableFrom(keyType))
                {
                    var keySchema = await schemaRegistry.GetLatestSchemaAsync(schemaRegistry.ConstructKeySubjectName(topic));
                    // retrieve schema and create deserializer
                    var methodInfo = typeof(SerializationHelper).GetMethod(nameof(CreateAvroKeyDeserializer), BindingFlags.Static | BindingFlags.NonPublic);
                    var genericMethod = methodInfo.MakeGenericMethod(valueType);
                    valueDeserializer = genericMethod.Invoke(null, new object[] { schemaRegistry });
                }
            }
            catch (SchemaRegistryException ex)
            {
                // if exception raises, key schema does not exist
                // do not throw an error and return basic confluent deserializer
                throw new TargetException($"Error finding avro schema for key in the schema registry via url. Exception: {ex.Message}");
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
                return ResolveSchemaRegistrySerializers(valueType, keyType, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword, topic).GetAwaiter().GetResult();
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
            if (string.IsNullOrWhiteSpace(specifiedValueAvroSchema) && isValueSpecificRecord)
            {
                specifiedValueAvroSchema = ((ISpecificRecord)Activator.CreateInstance(valueType)).Schema.ToString();
            }
            if (string.IsNullOrWhiteSpace(specifiedKeyAvroSchema) && isKeySpecificRecord)
            {
                specifiedKeyAvroSchema = ((ISpecificRecord)Activator.CreateInstance(keyType)).Schema.ToString();
            }

            // check for avro serialization
            if (specifiedValueAvroSchema != null || specifiedKeyAvroSchema != null)
            {
                // create schema registry client
                var schemaRegistry = CreateSchemaRegistry(specifiedValueAvroSchema, specifiedKeyAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);
                var serializer = Activator.CreateInstance(null);

                // create serializers for avro - generic or specific records
                if (isValueGenericRecord || isValueSpecificRecord)
                {
                    serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
                    valueSerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(valueType).Invoke(null, new object[] { serializer });
                }
                if (isKeyGenericRecord || isKeySpecificRecord)
                {
                    serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(keyType), schemaRegistry, null /* config */);
                    keySerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(keyType).Invoke(null, new object[] { serializer });
                }
            }

            return (valueSerializer, keySerializer);
        }

        internal static async Task<(object, object)> ResolveSchemaRegistrySerializers(Type valueType, Type keyType, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword, string topic)
        {
            object valueSerializer = null;
            object keySerializer = null;

            var schemaRegistry = CreateSchemaRegistry(null, null, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);

            // query schema registry to find existence of value and key schemas
            var valueSchema = await schemaRegistry.GetLatestSchemaAsync(schemaRegistry.ConstructValueSubjectName(topic));
            if (valueSchema != null)
            {
                var serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(valueType), schemaRegistry, null /* config */);
                valueSerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(valueType).Invoke(null, new object[] { serializer });
            }

            var keySchema = await schemaRegistry.GetLatestSchemaAsync(schemaRegistry.ConstructKeySubjectName(topic));
            if (keySchema != null)
            {
                var serializer = Activator.CreateInstance(typeof(AvroSerializer<>).MakeGenericType(keyType), schemaRegistry, null /* config */);
                keySerializer = typeof(SyncOverAsyncSerializerExtensionMethods).GetMethod("AsSyncOverAsync").MakeGenericMethod(keyType).Invoke(null, new object[] { serializer });
            }

            return (valueSerializer, keySerializer);
        }


        private static ISchemaRegistryClient CreateSchemaRegistry(string specifiedValueAvroSchema, string specifiedKeyAvroSchema, string schemaRegistryUrl, string schemaRegistryUsername, string schemaRegistryPassword)
        {
            if (!string.IsNullOrWhiteSpace(specifiedValueAvroSchema) || !string.IsNullOrWhiteSpace(specifiedKeyAvroSchema))
            {
                return new LocalSchemaRegistry(specifiedValueAvroSchema, specifiedKeyAvroSchema);
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
            throw new ArgumentNullException(nameof(specifiedValueAvroSchema), $@"parameter is required when creating an generic avro serializer");
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

                // if schema registry is present, the types must be generic too?

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
