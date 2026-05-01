// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Google.Protobuf;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Serialization;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaRecordProtobufSerializerTest
    {
        [Fact]
        public void Serialize_SingleEvent_ProducesValidProtobuf()
        {
            var eventData = new KafkaEventData<string, string>("myKey", "myValue")
            {
                Offset = 42,
                Partition = 3,
                Topic = "test-topic",
                Timestamp = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc),
                LeaderEpoch = 7,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 0);

            // Round-trip: deserialize and verify
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);
            Assert.Equal("test-topic", proto.Topic);
            Assert.Equal(3, proto.Partition);
            Assert.Equal(42, proto.Offset);
            Assert.Equal("myKey", Encoding.UTF8.GetString(proto.Key.ToByteArray()));
            Assert.Equal("myValue", Encoding.UTF8.GetString(proto.Value.ToByteArray()));
            Assert.NotNull(proto.Timestamp);
            Assert.True(proto.Timestamp.UnixTimestampMs > 0);
        }

        [Fact]
        public void Serialize_LeaderEpoch_NotIncludedInProtobuf()
        {
            // LeaderEpoch is consumer fetch metadata, not stored record data (issue #639).
            // Even when set on IKafkaEventData, it should NOT appear in the Protobuf output.
            var eventData = new KafkaEventData<string, string>("key", "value")
            {
                Offset = 1,
                Partition = 0,
                Topic = "topic",
                Timestamp = DateTime.UtcNow,
                LeaderEpoch = 42,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            Assert.False(ContainsTopLevelField(bytes, 8));

            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            // Protobuf schema no longer has leader_epoch field.
            // Verify the record still round-trips correctly without it.
            Assert.Equal("topic", proto.Topic);
            Assert.Equal(0, proto.Partition);
            Assert.Equal(1, proto.Offset);
        }

        [Fact]
        public void Serialize_NullKey_SerializesAsEmptyBytes()
        {
            var eventData = new KafkaEventData<string>("testValue")
            {
                Offset = 0,
                Partition = 0,
                Topic = "topic",
                Timestamp = DateTime.UtcNow,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.True(proto.Key.IsEmpty);
            Assert.Equal("testValue", Encoding.UTF8.GetString(proto.Value.ToByteArray()));
        }

        [Fact]
        public void Serialize_ByteArrayValue_PreservedExactly()
        {
            var binaryValue = new byte[] { 0x00, 0x01, 0xFF, 0xFE, 0x42 };
            var binaryKey = new byte[] { 0xAA, 0xBB };
            var eventData = new KafkaEventData<byte[], byte[]>(binaryKey, binaryValue)
            {
                Offset = 100,
                Partition = 1,
                Topic = "binary-topic",
                Timestamp = DateTime.UtcNow,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.Equal(binaryValue, proto.Value.ToByteArray());
            Assert.Equal(binaryKey, proto.Key.ToByteArray());
        }

        [Fact]
        public void Serialize_WithHeaders_AllPreserved()
        {
            var eventData = new KafkaEventData<string, string>("key", "value")
            {
                Offset = 5,
                Partition = 0,
                Topic = "headers-topic",
                Timestamp = DateTime.UtcNow,
            };
            eventData.Headers.Add("correlationId", Encoding.UTF8.GetBytes("abc-123"));
            eventData.Headers.Add("traceParent", Encoding.UTF8.GetBytes("00-trace"));

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.Equal(2, proto.Headers.Count);
            Assert.Equal("correlationId", proto.Headers[0].Key);
            Assert.Equal("abc-123", Encoding.UTF8.GetString(proto.Headers[0].Value.ToByteArray()));
            Assert.Equal("traceParent", proto.Headers[1].Key);
            Assert.Equal("00-trace", Encoding.UTF8.GetString(proto.Headers[1].Value.ToByteArray()));
        }

        [Fact]
        public void Serialize_NoHeaders_ProducesEmptyList()
        {
            var eventData = new KafkaEventData<string, string>("key", "value")
            {
                Offset = 0,
                Partition = 0,
                Topic = "topic",
                Timestamp = DateTime.UtcNow,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.Empty(proto.Headers);
        }

        [Fact]
        public void Serialize_TimestampPreserved()
        {
            var timestamp = new DateTime(2026, 3, 15, 14, 30, 45, DateTimeKind.Utc);
            var expectedMs = new DateTimeOffset(timestamp).ToUnixTimeMilliseconds();

            var eventData = new KafkaEventData<string, string>("key", "value")
            {
                Offset = 0,
                Partition = 0,
                Topic = "topic",
                Timestamp = timestamp,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.Equal(expectedMs, proto.Timestamp.UnixTimestampMs);
            Assert.Equal(0, proto.Timestamp.Type); // NotAvailable — IKafkaEventData doesn't preserve TimestampType
        }

        [Fact]
        public void BindingSource_IsAzureKafkaRecord()
        {
            Assert.Equal("AzureKafkaRecord", KafkaRecordProtobufSerializer.BindingSource);
        }

        [Fact]
        public void ContentType_IsProtobuf()
        {
            Assert.Equal("application/x-protobuf", KafkaRecordProtobufSerializer.ContentType);
        }

        [Fact]
        public void Serialize_NullValue_SerializesAsEmptyBytes()
        {
            var eventData = new KafkaEventData<string>()
            {
                Offset = 0,
                Partition = 0,
                Topic = "topic",
                Timestamp = DateTime.UtcNow,
            };

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.True(proto.Value.IsEmpty);
        }

        [Fact]
        public void Serialize_HeaderWithNullValue_PreservesNullAsEmpty()
        {
            var eventData = new KafkaEventData<string, string>("key", "value")
            {
                Offset = 0,
                Partition = 0,
                Topic = "topic",
                Timestamp = DateTime.UtcNow,
            };
            eventData.Headers.Add("nullHeader", null);

            var bytes = KafkaRecordProtobufSerializer.Serialize(eventData);
            var proto = KafkaRecordProto.Parser.ParseFrom(bytes);

            Assert.Single(proto.Headers);
            Assert.Equal("nullHeader", proto.Headers[0].Key);
            Assert.True(proto.Headers[0].Value.IsEmpty);
        }

        private static bool ContainsTopLevelField(byte[] bytes, int fieldNumber)
        {
            var input = new CodedInputStream(bytes);
            uint tag;

            while ((tag = input.ReadTag()) != 0)
            {
                if (WireFormat.GetTagFieldNumber(tag) == fieldNumber)
                {
                    return true;
                }

                input.SkipLastField();
            }

            return false;
        }
    }
}
