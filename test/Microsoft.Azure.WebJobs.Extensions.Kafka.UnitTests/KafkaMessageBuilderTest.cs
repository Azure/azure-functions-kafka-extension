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

            Assert.StartsWith("Could not cast actual key value to the expected", ex.Message);
        }

        [Theory]
        [InlineData("testKey", "testValue", "testKey", "testValue")] // string type
        [InlineData("12", "testValue", 12, "testValue")] // int type
        [InlineData("testKey", "testValue", new byte[] { 116, 101, 115, 116, 75, 101, 121}, "testValue")] // binary type
        [InlineData("12", "testValue", 12L, "testValue")] // long type
        public void When_TKey_Is_Different_Type_Should_Cast_Key_To_That_Type<TKey, TValue>(string key, string value, TKey expectedKey, TValue expectedValue)
        {
            var toBuildFromEvent = new KafkaEventData<string, string>(key, value);
            var expectedResult = new KafkaEventData<TKey, TValue>(expectedKey, expectedValue);

            var builder = new KafkaMessageBuilder<TKey, TValue>();
            var msg = builder.BuildFrom(toBuildFromEvent);

            // assert equal key and value
            Assert.Equal(expectedResult.Key, msg.Key);
            Assert.Equal(expectedResult.Value, msg.Value);
        }
    }
}
