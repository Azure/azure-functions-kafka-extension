// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaAsyncCollectorTests
    {
        [Fact]
        public void Producer_NullArgumentCheck()
        {
            var mockProducer = new Mock<IKafkaProducer>();
            Assert.Throws<ArgumentNullException>(() => new KafkaAsyncCollector("topic", null));
        }

        [Fact]
        public async Task SendMultipleProduce()
        {
            var mockProducer = new Mock<IKafkaProducer>();
            mockProducer.Setup(x => x.ProduceAsync("topic", It.IsNotNull<KafkaEventData>()))
                .Returns(Task.CompletedTask);

            var collector = new KafkaAsyncCollector("topic", mockProducer.Object);


            await collector.AddAsync(new KafkaEventData()
            {
                Key = 123,
                Value = "hello world"
            });
            await collector.AddAsync(new KafkaEventData()
            {
                Key = 1234,
                Value = "hello world 2"
            });

            await collector.FlushAsync();

            mockProducer.Verify(x => x.ProduceAsync("topic", It.IsNotNull<KafkaEventData>()), Times.Exactly(2));
            mockProducer.Verify(x => x.ProduceAsync("topic", It.Is<KafkaEventData>(k => k.Value.ToString() == "hello world")), Times.Once);
            mockProducer.Verify(x => x.ProduceAsync("topic", It.Is<KafkaEventData>(k => k.Value.ToString() == "hello world 2")), Times.Once);
        }
        
        [Fact]
        public async Task CantSendNullEvent()
        {
            var mockProducer = new Mock<IKafkaProducer>();
            var collector = new KafkaAsyncCollector("topic", mockProducer.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await collector.AddAsync(null);
            });
        }
    }
}
