// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaMessageBuilderTest
    {
        [Fact]
        public void When_Keyed_Event_Has_Headers_Recreates_Them_In_Kafka_Message()
        {
            var eventData = new KafkaEventData<string, string>
            {
                Headers =
                {
                    { "test", Encoding.UTF8.GetBytes("test") },
                    { "test", Encoding.UTF8.GetBytes("test2") }
                }
            };

            var builder = new KafkaMessageBuilder<string, string>();

            var msg = builder.BuildFrom(eventData);

            Assert.NotNull(msg);
            Assert.NotNull(msg.Headers);
            Assert.Equal(2, msg.Headers.Count);
            Assert.Equal("test", msg.Headers[0].Key);
            Assert.Equal(Encoding.UTF8.GetBytes("test"), msg.Headers[0].GetValueBytes());
            Assert.Equal("test", msg.Headers[1].Key);
            Assert.Equal(Encoding.UTF8.GetBytes("test2"), msg.Headers[1].GetValueBytes());
        }

        [Fact]
        public void When_NonKeyed_Event_Has_Headers_Recreates_Them_In_Kafka_Message()
        {
            var eventData = new KafkaEventData<string>
            {
                Headers =
                {
                    { "test", Encoding.UTF8.GetBytes("test") },
                    { "test", Encoding.UTF8.GetBytes("test2") }
                }
            };

            var builder = new KafkaMessageBuilder<string, string>();

            var msg = builder.BuildFrom(eventData);

            Assert.NotNull(msg);
            Assert.NotNull(msg.Headers);
            Assert.Equal(2, msg.Headers.Count);
            Assert.Equal("test", msg.Headers[0].Key);
            Assert.Equal(Encoding.UTF8.GetBytes("test"), msg.Headers[0].GetValueBytes());
            Assert.Equal("test", msg.Headers[1].Key);
            Assert.Equal(Encoding.UTF8.GetBytes("test2"), msg.Headers[1].GetValueBytes());
        }

        [Fact]
        public void When_Event_Has_No_Headers_No_Headers_Are_Added_To_Message()
        {
            var eventData = new KafkaEventData<string> { };

            var builder = new KafkaMessageBuilder<string, string>();

            var msg = builder.BuildFrom(eventData);

            Assert.NotNull(msg);
            Assert.Null(msg.Headers);
        }

        [Fact]
        public void When_Event_Has_Key_Of_Wrong_Type_Should_Fail()
        {
            var eventData = new KafkaEventData<string, string>("test", "test");

            var builder = new KafkaMessageBuilder<int, string>();

            var ex = Assert.Throws<ArgumentException>(()=> builder.BuildFrom(eventData));

            Assert.StartsWith("Key value is not of the expected type", ex.Message);
        }
    }
}
