// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Moq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.output
{
    public class KafkaProducerAsyncCollectorTest
    {
        private readonly string testValue = "test";
        private Mock<IKafkaProducerFactory> kafkaProducerFactory = new Mock<IKafkaProducerFactory>();
        private KafkaProducerEntity kafkaProducerEntity = new KafkaProducerEntity();
        private Mock<IKafkaProducer> kafkaProducer = new Mock<IKafkaProducer>();
        private readonly string jsonStringValueHeader = "{\r\n  \"Offset\": 0,\r\n  \"Partition\": 0,\r\n  \"Topic\": \"\",\r\n  \"Timestamp\": \"Wed, 30 Mar 2022 05:56:20 GMT\",\r\n  \"Value\": \"shiv shambhu\",\r\n  \"Headers\": [\r\n    {\r\n      \"Key\": \"test\",\r\n      \"Value\": \"1\"\r\n    },\r\n    {\r\n      \"Key\": \"test1\",\r\n      \"Value\": \"2\"\r\n    }\r\n  ]\r\n}";
        private readonly string jsonStringValue = "{\r\n  \"Offset\": 0,\r\n  \"Partition\": 0,\r\n  \"Topic\": \"\",\r\n  \"Timestamp\": \"Wed, 30 Mar 2022 05:56:20 GMT\",\r\n  \"Value\": \"shiv shambhu\",\r\n  \"Headers\": []\r\n}";

        [Fact]
        public async Task AddAsync_Item_Is_NullAsync()
        {
            var kafkaProducerEntity = new Mock<KafkaProducerEntity>();
            IAsyncCollector<string> asyncCollector = new KafkaProducerAsyncCollector<string>(
                kafkaProducerEntity.Object, Guid.NewGuid());
            await Assert.ThrowsAsync<InvalidOperationException>(() => asyncCollector.AddAsync(null, default));
        }

        [Fact]
        public void AddAsync_Item_Is_Of_Bytes_Types()
        {
            BuildMockData();
            IAsyncCollector<byte[]> asyncCollector = new KafkaProducerAsyncCollector<byte[]>(
                kafkaProducerEntity, Guid.NewGuid());
            KafkaEventData<byte[]> kafkaEventData = new KafkaEventData<byte[]>(Encoding.UTF8.GetBytes(testValue));

            Task task = asyncCollector.AddAsync(Encoding.UTF8.GetBytes(testValue), default);
            Assert.True(task.IsCompleted);
        }

        private void BuildMockData()
        {
            kafkaProducerEntity.KafkaProducerFactory = kafkaProducerFactory.Object;
            kafkaProducerFactory.Setup(e => e.Create(It.IsAny<KafkaProducerEntity>())).Returns(kafkaProducer.Object);
            kafkaProducer.Setup(prod => prod.ProduceAsync(It.IsAny<string>(), It.IsAny<Object>()));
        }

        [Fact]
        public void AddAsync_Item_Is_Of_String_Value_Types()
        {
            BuildMockData();
            IAsyncCollector<string> asyncCollector = new KafkaProducerAsyncCollector<string>(
                kafkaProducerEntity, Guid.NewGuid());
            KafkaEventData<string> kafkaEventData = new KafkaEventData<string>(testValue);
            Task task = asyncCollector.AddAsync(testValue, default);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void AddAsync_Item_Is_Of_KafkaEventData_Json_String_Types()
        {
            BuildMockData();

            IAsyncCollector<string> asyncCollector = new KafkaProducerAsyncCollector<string>(
                kafkaProducerEntity, Guid.NewGuid());
            Task task = asyncCollector.AddAsync(jsonStringValue, default);
            Assert.True(task.IsCompleted);
        }
        [Fact]
        public void AddAsync_Item_Is_Of_KafkaEventData_Json_String_Header_Types()
        {
            BuildMockData();

            IAsyncCollector<string> asyncCollector = new KafkaProducerAsyncCollector<string>(
                kafkaProducerEntity, Guid.NewGuid());
            Task task = asyncCollector.AddAsync(jsonStringValueHeader, default);
            Assert.True(task.IsCompleted);
        }
    }
}
