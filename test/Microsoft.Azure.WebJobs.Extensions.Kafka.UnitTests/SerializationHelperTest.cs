// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry.Serdes;
using Google.Protobuf;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    /// <summary>
    /// Tests for SerializationHelper, focusing on SchemaRegistryUrl handling for Out-of-proc scenarios.
    /// Issue #532: Schema mismatch error when using SchemaRegistryUrl with dotnet isolated.
    /// </summary>
    public class SerializationHelperTest
    {
        /// <summary>
        /// Test that when SchemaRegistryUrl is provided and valueType is string (Out-of-proc scenario),
        /// the deserializer should still be created for GenericRecord to properly deserialize Avro messages.
        /// This is the core fix for Issue #532.
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WhenValueTypeIsString_ShouldCreateGenericRecordDeserializer()
        {
            // Arrange
            var valueType = typeof(string);  // Out-of-proc default type
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - The fix should create a GenericRecord deserializer even for string valueType
            Assert.NotNull(valueDeserializer);
            Assert.IsType<SyncOverAsyncDeserializer<GenericRecord>>(valueDeserializer);
        }

        /// <summary>
        /// Test that when SchemaRegistryUrl is provided and valueType is byte[] (another Out-of-proc scenario),
        /// the deserializer should still be created for GenericRecord.
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WhenValueTypeIsByteArray_ShouldCreateGenericRecordDeserializer()
        {
            // Arrange
            var valueType = typeof(byte[]);  // Another Out-of-proc type
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - The fix should create a GenericRecord deserializer even for byte[] valueType
            Assert.NotNull(valueDeserializer);
            Assert.IsType<SyncOverAsyncDeserializer<GenericRecord>>(valueDeserializer);
        }

        /// <summary>
        /// Verify that existing behavior for GenericRecord is preserved.
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WhenValueTypeIsGenericRecord_ShouldCreateGenericRecordDeserializer()
        {
            // Arrange
            var valueType = typeof(GenericRecord);  // In-proc with GenericRecord
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - Existing behavior should be preserved
            Assert.NotNull(valueDeserializer);
            Assert.IsType<SyncOverAsyncDeserializer<GenericRecord>>(valueDeserializer);
        }

        /// <summary>
        /// Verify that ISpecificRecord types are NOT affected by this fix (they should continue to work as before).
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WhenValueTypeIsSpecificRecord_ShouldNotCreateDeserializer()
        {
            // Arrange
            var valueType = typeof(MyAvroRecord);  // ISpecificRecord type
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - ISpecificRecord should NOT get a deserializer from this method
            // (it's handled elsewhere with the specific record's schema)
            Assert.Null(valueDeserializer);
        }

        /// <summary>
        /// Test ResolveDeserializers with SchemaRegistryUrl and string valueType (full flow).
        /// </summary>
        [Fact]
        public void ResolveDeserializers_WithSchemaRegistryUrl_AndStringValueType_ShouldCreateGenericRecordDeserializer()
        {
            // Arrange
            var keyAndValueTypes = new SerializationHelper.GetKeyAndValueTypesResult
            {
                ValueType = typeof(string),
                KeyType = typeof(string),
                ValueAvroSchema = null,
                KeyAvroSchema = null,
                RequiresKey = false
            };
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveDeserializers(
                keyAndValueTypes,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert
            Assert.NotNull(valueDeserializer);
            Assert.IsType<SyncOverAsyncDeserializer<GenericRecord>>(valueDeserializer);
        }

        /// <summary>
        /// Verify that Protobuf types are NOT affected by this fix.
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WhenValueTypeIsProtobuf_ShouldNotCreateDeserializer()
        {
            // Arrange
            var valueType = typeof(ProtoUser);  // IMessage (Protobuf) type
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - Protobuf types should NOT get an Avro deserializer
            Assert.Null(valueDeserializer);
        }

        /// <summary>
        /// Test with authentication credentials.
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WithCredentials_ShouldCreateGenericRecordDeserializer()
        {
            // Arrange
            var valueType = typeof(string);
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            var schemaRegistryUsername = "testuser";
            var schemaRegistryPassword = "testpassword";
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - Should still create deserializer with credentials
            Assert.NotNull(valueDeserializer);
            Assert.IsType<SyncOverAsyncDeserializer<GenericRecord>>(valueDeserializer);
        }

        /// <summary>
        /// Test that object type (another potential Out-of-proc type) creates GenericRecord deserializer.
        /// </summary>
        [Fact]
        public void ResolveSchemaRegistryDeserializers_WhenValueTypeIsObject_ShouldCreateGenericRecordDeserializer()
        {
            // Arrange
            var valueType = typeof(object);
            var keyType = typeof(string);
            var schemaRegistryUrl = "localhost:8081";
            string schemaRegistryUsername = null;
            string schemaRegistryPassword = null;
            var topic = "test-topic";

            // Act
            var (valueDeserializer, keyDeserializer) = SerializationHelper.ResolveSchemaRegistryDeserializers(
                valueType,
                keyType,
                schemaRegistryUrl,
                schemaRegistryUsername,
                schemaRegistryPassword,
                topic);

            // Assert - object type should get GenericRecord deserializer
            Assert.NotNull(valueDeserializer);
            Assert.IsType<SyncOverAsyncDeserializer<GenericRecord>>(valueDeserializer);
        }
    }
}
