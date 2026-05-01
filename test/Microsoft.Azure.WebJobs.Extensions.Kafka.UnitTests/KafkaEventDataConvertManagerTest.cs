// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Serialization;
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
            Assert.Equal(KafkaRecordProtobufSerializer.BindingSource, bindingData.Source);
            Assert.Equal(KafkaRecordProtobufSerializer.ContentType, bindingData.ContentType);
            Assert.False(ProtobufTestHelpers.ContainsTopLevelField(bindingData.Content.ToArray(), 8));

            // Verify content is valid Protobuf with expected fields
            var proto = KafkaRecordProto.Parser.ParseFrom(bindingData.Content);
            Assert.Equal("test-topic", proto.Topic);
            Assert.Equal(3, proto.Partition);
            Assert.Equal(42L, proto.Offset);
            // LeaderEpoch intentionally not serialized into KafkaRecordProto (issue #639)
            Assert.Equal("test-key", Encoding.UTF8.GetString(proto.Key.ToByteArray()));
            Assert.Equal("test-value", Encoding.UTF8.GetString(proto.Value.ToByteArray()));

            // Headers
            Assert.Single(proto.Headers);
            Assert.Equal("h1", proto.Headers[0].Key);
            Assert.Equal("v1", Encoding.UTF8.GetString(proto.Headers[0].Value.ToByteArray()));
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
