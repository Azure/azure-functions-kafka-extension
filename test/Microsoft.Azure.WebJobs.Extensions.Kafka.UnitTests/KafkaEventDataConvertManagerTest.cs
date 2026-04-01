// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaEventDataConvertManagerTest
    {
        [Fact]
        public void GetConverter_IKafkaEventData_To_ParameterBindingData_ReturnsConverter()
        {
            var manager = new KafkaEventDataConvertManager(NullLogger.Instance);
            var converter = manager.GetConverter<KafkaTriggerAttribute>(typeof(IKafkaEventData), typeof(ParameterBindingData));
            Assert.NotNull(converter);
        }

        [Fact]
        public void GetConverter_KafkaEventDataGeneric_To_ParameterBindingData_ReturnsConverter()
        {
            var manager = new KafkaEventDataConvertManager(NullLogger.Instance);
            var converter = manager.GetConverter<KafkaTriggerAttribute>(typeof(KafkaEventData<string, string>), typeof(ParameterBindingData));
            Assert.NotNull(converter);
        }

        [Fact]
        public async Task ConvertToParameterBindingData_ProducesCorrectBindingData()
        {
            var manager = new KafkaEventDataConvertManager(NullLogger.Instance);
            var converter = manager.GetConverter<KafkaTriggerAttribute>(typeof(IKafkaEventData), typeof(ParameterBindingData));

            var eventData = new KafkaEventData<string, string>()
            {
                Key = "test-key",
                Value = "test-value",
                Offset = 42,
                Partition = 3,
                Topic = "test-topic",
                Timestamp = new DateTime(2026, 3, 16, 12, 0, 0, DateTimeKind.Utc),
                LeaderEpoch = 5,
                IsPartitionEOF = false
            };
            eventData.Headers.Add("h1", Encoding.UTF8.GetBytes("v1"));

            var result = await converter(eventData, new KafkaTriggerAttribute("test-topic", "broker"), null);

            Assert.IsType<ParameterBindingData>(result);
            var bindingData = (ParameterBindingData)result;
            Assert.Equal("1.0", bindingData.Version);
            Assert.Equal(KafkaRecordSerializer.BindingSource, bindingData.Source);
            Assert.Equal(KafkaRecordSerializer.JsonContentType, bindingData.ContentType);

            // Verify content is valid JSON with expected fields
            var json = JObject.Parse(bindingData.Content.ToString());
            Assert.Equal("test-topic", json["topic"].Value<string>());
            Assert.Equal(3, json["partition"].Value<int>());
            Assert.Equal(42L, json["offset"].Value<long>());
            Assert.Equal(5, json["leaderEpoch"].Value<int>());
            Assert.False(json["isPartitionEOF"].Value<bool>());
            Assert.Equal("test-key", json["message"]["key"].Value<string>());
            Assert.Equal("test-value", json["message"]["value"].Value<string>());

            // Headers
            var headers = json["message"]["headers"] as JArray;
            Assert.Single(headers);
            Assert.Equal("h1", headers[0]["key"].Value<string>());
        }

        [Fact]
        public void GetConverter_IKafkaEventData_To_String_ReturnsConverter()
        {
            var manager = new KafkaEventDataConvertManager(NullLogger.Instance);
            var converter = manager.GetConverter<KafkaTriggerAttribute>(typeof(IKafkaEventData), typeof(string));
            Assert.NotNull(converter);
        }

        [Fact]
        public void GetConverter_IKafkaEventData_To_ByteArray_ReturnsConverter()
        {
            var manager = new KafkaEventDataConvertManager(NullLogger.Instance);
            var converter = manager.GetConverter<KafkaTriggerAttribute>(typeof(IKafkaEventData), typeof(byte[]));
            Assert.NotNull(converter);
        }
    }
}
