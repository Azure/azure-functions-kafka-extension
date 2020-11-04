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
    }
}
