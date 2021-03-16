// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaEventDataHeadersTest
    {
        [Fact]
        public void EmptyReadOnly_Is_ReadOnly()
        {
            Assert.True(KafkaEventDataHeaders.EmptyReadOnly.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => KafkaEventDataHeaders.EmptyReadOnly.Add("test", null));
        }

        [Fact]
        public void When_Created_As_ReadOnly_Is_ReadOnly()
        {
            var headers = new KafkaEventDataHeaders(true);
            
            Assert.True(headers.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => headers.Add("test", null));
        }

        [Fact]
        public void Add_Throws_When_Key_Is_Null()
        {
            var headers = new KafkaEventDataHeaders();
            
            Assert.Throws<ArgumentNullException>(() => headers.Add(null, null));
        }

        [Fact]
        public void Add_Adds_Header()
        {
            var headers = new KafkaEventDataHeaders();
            
            headers.Add("test", Encoding.UTF8.GetBytes("test"));
            
            Assert.Equal(1, headers.Count);
        }

        [Fact]
        public void Remove_Removes_Header()
        {
            var headers = new KafkaEventDataHeaders();
            
            headers.Add("test", Encoding.UTF8.GetBytes("test"));
            headers.Add("test2", Encoding.UTF8.GetBytes("test"));
            headers.Remove("test2");
            
            Assert.Equal(1, headers.Count);
        }

        [Fact]
        public void GetLast_Returns_The_Last_Header_Value()
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));
            headers.Add("test", Encoding.UTF8.GetBytes("testValue2"));

            var value = headers.GetLast("test");
            
            Assert.Equal("testValue2", Encoding.UTF8.GetString(value));
        }

        [Fact]
        public void GetFirst_Returns_The_Last_Header_Value()
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));
            headers.Add("test", Encoding.UTF8.GetBytes("testValue2"));

            var value = headers.GetFirst("test");
            
            Assert.Equal("testValue1", Encoding.UTF8.GetString(value));
        }

        [Fact]
        public void GetLast_Throws_On_InvalidKey()
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));
           
            Assert.Throws<KeyNotFoundException>(()=> headers.GetLast("test2"));
        }

        [Fact]
        public void GetFirst_Throws_On_InvalidKey()
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));

            Assert.Throws<KeyNotFoundException>(() => headers.GetFirst("test2"));
        }

        [Theory]
        [InlineData("test", true, "testValue2")]
        [InlineData("test2", false, default(string))]
        public void TryGetLast_Outputs_The_Last_Header_Value(string key, bool expectedSuccess, string output)
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));
            headers.Add("test", Encoding.UTF8.GetBytes("testValue2"));

            var success = headers.TryGetLast(key, out var value);

            Assert.Equal(expectedSuccess, success);
            Assert.Equal(output, value == null ? null : Encoding.UTF8.GetString(value));
        }

        [Theory]
        [InlineData("test", true, "testValue1")]
        [InlineData("test2", false, default(string))]
        public void TryGetFirst_Outputs_The_Last_Header_Value(string key, bool expectedSuccess, string output)
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));
            headers.Add("test", Encoding.UTF8.GetBytes("testValue2"));

            var success = headers.TryGetFirst(key, out var value);
            
            Assert.Equal(expectedSuccess, success); 
            Assert.Equal(output, value == null ? null : Encoding.UTF8.GetString(value));
        }

        [Fact]
        public void Can_Be_Enumerated()
        {
            var headers = new KafkaEventDataHeaders();
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));
            headers.Add("test", Encoding.UTF8.GetBytes("testValue1"));

            Assert.Collection(headers, x => Assert.Equal("test", x.Key), x => Assert.Equal("test", x.Key));
        }
    }
}
