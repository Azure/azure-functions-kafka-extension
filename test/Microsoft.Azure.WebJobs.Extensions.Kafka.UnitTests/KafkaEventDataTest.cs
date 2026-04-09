// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaEventDataTest
    {
        [Fact]
        public void When_KafkaEventData_Is_Created_With_Empty_Constructor_The_Headers_Can_Be_Set()
        {
            var eventData = new KafkaEventData<string>
            {
                Headers =
                {
                    { "testKey", Encoding.UTF8.GetBytes("testValue") }
                }
            };

            Assert.Equal("testValue", Encoding.UTF8.GetString(eventData.Headers.GetFirst("testKey")));
        }

        [Fact]
        public void When_KeyedKafkaEventData_Is_Created_With_Empty_Constructor_The_Headers_Can_Be_Set()
        {
            var eventData = new KafkaEventData<string, string>
            {
                Headers =
                {
                    { "testKey", Encoding.UTF8.GetBytes("testValue") }
                }
            };

            Assert.Equal("testValue", Encoding.UTF8.GetString(eventData.Headers.GetFirst("testKey")));
        }

        [Fact]
        public void When_KeyedKafkaEventData_Is_Created_With_Value_Constructor_The_Headers_Can_Be_Set()
        {
            var eventData = new KafkaEventData<string, string>("key", "value")
            {
                Headers =
                {
                    { "testKey", Encoding.UTF8.GetBytes("testValue") }
                }
            };
            Assert.Equal("key", eventData.Key);
            Assert.Equal("value", eventData.Value);
            Assert.Equal("testValue", Encoding.UTF8.GetString(eventData.Headers.GetFirst("testKey")));
        }

        [Fact]
        public void When_KafkaEventData_Is_Created_From_IKafkaEventData_The_Headers_Keep_The_Writeability()
        {
            var eventDataWithReadWrite = new KafkaEventData<string>(new KafkaEventData<string>());
            var eventDataWithReadOnly = new KafkaEventData<string>(new KafkaEventData<string>(KafkaEventDataHeaders.EmptyReadOnly));

            var ex = Record.Exception(() => eventDataWithReadWrite.Headers.Add("test", null));

            Assert.Null(ex);

            Assert.Throws<NotSupportedException>(() => eventDataWithReadOnly.Headers.Add("test", null));
        }

        [Fact]
        public void When_KafkaEventData_Is_Created_From_ConsumeResult_The_Headers_Can_Not_Be_Set()
        {
            var message = new Message<string, string> { Headers = new Headers() };
            message.Headers.Add("testKey", null);
            var consumeResult = new ConsumeResult<string, string> { Message = message };

            var eventData = KafkaEventData<string>.CreateFrom(consumeResult);

            Assert.Equal(1, eventData.Headers.Count);
            Assert.Throws<NotSupportedException>(() => eventData.Headers.Add("test", null));
        }

        [Fact]
        public void When_KeyedKafkaEventData_Is_Created_From_ConsumeResult_The_Headers_Can_Not_Be_Set()
        {
            var message = new Message<string, string> { Headers = new Headers()};
            message.Headers.Add("testKey", null);
            var consumeResult = new ConsumeResult<string, string> { Message = message };

            var eventData = new KafkaEventData<string, string>(consumeResult);

            Assert.Equal(1, eventData.Headers.Count);
            Assert.Throws<NotSupportedException>(() => eventData.Headers.Add("test", null));
        }

        [Fact]
        public void When_KafkaEventData_Is_Created_From_ConsumeResult_Without_Headers_The_Headers_Are_Static()
        {
            var message = new Message<string, string>();
            
            var consumeResult = new ConsumeResult<string, string> { Message = message };

            var eventData = KafkaEventData<string>.CreateFrom(consumeResult);

            Assert.Same(KafkaEventDataHeaders.EmptyReadOnly, eventData.Headers);
            Assert.Throws<NotSupportedException>(() => eventData.Headers.Add("test", null));
        }

        [Fact]
        public void When_KeyedKafkaEventData_Is_Created_From_ConsumeResult_Without_Headers_The_Headers_Are_Static()
        {
            var message = new Message<string, string>();

            var consumeResult = new ConsumeResult<string, string> { Message = message };

            var eventData = new KafkaEventData<string, string>(consumeResult);

            Assert.Same(KafkaEventDataHeaders.EmptyReadOnly, eventData.Headers);
            Assert.Throws<NotSupportedException>(() => eventData.Headers.Add("test", null));
        }

        [Fact]
        public void When_KeyedKafkaEventData_Is_Created_From_ConsumeResult_LeaderEpoch_And_IsPartitionEOF_Are_Preserved()
        {
            var message = new Message<string, string>
            {
                Key = "key1",
                Value = "value1",
                Timestamp = new Timestamp(new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc))
            };
            var consumeResult = new ConsumeResult<string, string>
            {
                Message = message,
                Topic = "test-topic",
                Partition = 2,
                Offset = 42,
                LeaderEpoch = 7,
                IsPartitionEOF = false
            };

            var eventData = new KafkaEventData<string, string>(consumeResult);

            Assert.Equal(7, eventData.LeaderEpoch);
            Assert.False(eventData.IsPartitionEOF);
            Assert.Equal("key1", eventData.Key);
            Assert.Equal("value1", eventData.Value);
            Assert.Equal(42L, eventData.Offset);
            Assert.Equal(2, eventData.Partition);
            Assert.Equal("test-topic", eventData.Topic);
        }

        [Fact]
        public void When_KafkaEventData_Is_Created_From_ConsumeResult_LeaderEpoch_And_IsPartitionEOF_Are_Preserved()
        {
            var message = new Message<string, string>
            {
                Key = "key1",
                Value = "value1",
                Timestamp = new Timestamp(new DateTime(2026, 3, 16, 0, 0, 0, DateTimeKind.Utc))
            };
            var consumeResult = new ConsumeResult<string, string>
            {
                Message = message,
                Topic = "test-topic",
                Partition = 3,
                Offset = 99,
                LeaderEpoch = null,
                IsPartitionEOF = true
            };

            var eventData = KafkaEventData<string>.CreateFrom(consumeResult);

            Assert.Null(eventData.LeaderEpoch);
            Assert.True(eventData.IsPartitionEOF);
            Assert.Equal("value1", eventData.Value);
            Assert.Equal(99L, eventData.Offset);
            Assert.Equal(3, eventData.Partition);
        }

        [Fact]
        public void When_KafkaEventData_Is_Created_From_IKafkaEventData_LeaderEpoch_And_IsPartitionEOF_Are_Copied()
        {
            var source = new KafkaEventData<string, string>()
            {
                Key = "k",
                Value = "v",
                Offset = 10,
                Partition = 1,
                Topic = "t",
                LeaderEpoch = 4,
                IsPartitionEOF = false
            };

            var copy = new KafkaEventData<string>(source);

            Assert.Equal(4, copy.LeaderEpoch);
            Assert.False(copy.IsPartitionEOF);
        }

        [Fact]
        public void When_KafkaEventData_LeaderEpoch_Defaults_To_Null_And_IsPartitionEOF_Defaults_To_False()
        {
            var eventData = new KafkaEventData<string, string>();

            Assert.Null(eventData.LeaderEpoch);
            Assert.False(eventData.IsPartitionEOF);

            var eventData2 = new KafkaEventData<string>();

            Assert.Null(eventData2.LeaderEpoch);
            Assert.False(eventData2.IsPartitionEOF);
        }
    }
}
