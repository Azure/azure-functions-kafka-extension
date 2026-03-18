// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Confluent.Kafka;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaRecordSerializerTest
    {
        [Fact]
        public void Serialize_SingleEvent_ProducesExpectedJsonStructure()
        {
            var eventData = new KafkaEventData<string, string>()
            {
                Key = "user-123",
                Value = "hello world",
                Offset = 12345,
                Partition = 3,
                Topic = "my-topic",
                Timestamp = new DateTime(2026, 3, 16, 12, 0, 0, DateTimeKind.Utc),
                LeaderEpoch = 7,
                IsPartitionEOF = false
            };
            eventData.Headers.Add("correlation-id", Encoding.UTF8.GetBytes("abc"));

            var bytes = KafkaRecordSerializer.Serialize(eventData);
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));

            Assert.Equal("my-topic", json["topic"].Value<string>());
            Assert.Equal(3, json["partition"].Value<int>());
            Assert.Equal(12345L, json["offset"].Value<long>());
            Assert.Equal(7, json["leaderEpoch"].Value<int>());
            Assert.False(json["isPartitionEOF"].Value<bool>());

            var message = json["message"];
            Assert.NotNull(message);
            Assert.Equal("user-123", message["key"].Value<string>());
            Assert.Equal("hello world", message["value"].Value<string>());

            // Timestamp
            var ts = message["timestamp"];
            Assert.NotNull(ts);
            Assert.True(ts["unixTimestampMs"].Value<long>() > 0);

            // Headers
            var headers = message["headers"] as JArray;
            Assert.NotNull(headers);
            Assert.Single(headers);
            Assert.Equal("correlation-id", headers[0]["key"].Value<string>());
            // Header value should be base64-encoded
            var headerValue = Convert.FromBase64String(headers[0]["value"].Value<string>());
            Assert.Equal("abc", Encoding.UTF8.GetString(headerValue));
        }

        [Fact]
        public void Serialize_NullLeaderEpoch_SerializesAsNull()
        {
            var eventData = new KafkaEventData<string, string>()
            {
                Key = "k",
                Value = "v",
                Offset = 1,
                Partition = 0,
                Topic = "t",
                Timestamp = DateTime.UtcNow,
                LeaderEpoch = null,
                IsPartitionEOF = true
            };

            var bytes = KafkaRecordSerializer.Serialize(eventData);
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));

            Assert.Null(json["leaderEpoch"].Value<int?>());
            Assert.True(json["isPartitionEOF"].Value<bool>());
        }

        [Fact]
        public void Serialize_NoHeaders_ProducesEmptyHeadersArray()
        {
            var eventData = new KafkaEventData<string, string>()
            {
                Key = "k",
                Value = "v",
                Offset = 0,
                Partition = 0,
                Topic = "t",
                Timestamp = DateTime.UtcNow
            };

            var bytes = KafkaRecordSerializer.Serialize(eventData);
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));

            var headers = json["message"]["headers"] as JArray;
            Assert.NotNull(headers);
            Assert.Empty(headers);
        }

        [Fact]
        public void Serialize_ByteArrayValue_SerializesAsBase64()
        {
            var eventData = new KafkaEventData<string, byte[]>()
            {
                Key = "k",
                Value = new byte[] { 0x01, 0x02, 0x03 },
                Offset = 0,
                Partition = 0,
                Topic = "t",
                Timestamp = DateTime.UtcNow
            };

            var bytes = KafkaRecordSerializer.Serialize(eventData);
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));

            var value = json["message"]["value"].Value<string>();
            var decoded = Convert.FromBase64String(value);
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, decoded);
        }

        [Fact]
        public void Serialize_NullKey_SerializesKeyAsNull()
        {
            var eventData = new KafkaEventData<string>()
            {
                Value = "v",
                Offset = 10,
                Partition = 1,
                Topic = "t",
                Timestamp = DateTime.UtcNow,
                Key = null
            };

            var bytes = KafkaRecordSerializer.Serialize(eventData);
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));

            Assert.Equal(JTokenType.Null, json["message"]["key"].Type);
        }

        [Fact]
        public void Serialize_MultipleHeaders_AllPreserved()
        {
            var eventData = new KafkaEventData<string, string>()
            {
                Key = "k",
                Value = "v",
                Offset = 0,
                Partition = 0,
                Topic = "t",
                Timestamp = DateTime.UtcNow
            };
            eventData.Headers.Add("h1", Encoding.UTF8.GetBytes("v1"));
            eventData.Headers.Add("h2", Encoding.UTF8.GetBytes("v2"));
            eventData.Headers.Add("h3", null);

            var bytes = KafkaRecordSerializer.Serialize(eventData);
            var json = JObject.Parse(Encoding.UTF8.GetString(bytes));

            var headers = json["message"]["headers"] as JArray;
            Assert.Equal(3, headers.Count);
            Assert.Equal("h1", headers[0]["key"].Value<string>());
            Assert.Equal("h2", headers[1]["key"].Value<string>());
            Assert.Equal("h3", headers[2]["key"].Value<string>());
            Assert.Equal(JTokenType.Null, headers[2]["value"].Type);
        }
    }
}
