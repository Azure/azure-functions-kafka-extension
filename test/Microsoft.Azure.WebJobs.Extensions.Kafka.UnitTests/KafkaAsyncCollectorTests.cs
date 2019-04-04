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
        public void NullArgumentCheck()
        {
            var mockProducer = new Mock<IKafkaProducer>();
            Assert.Throws<ArgumentNullException>(() => new KafkaAsyncCollector(null, mockProducer.Object));
        }

        [Fact]
        public async Task SendMultipleProduce()
        {
            var mockProducer = new Mock<IKafkaProducer>();
            var cancelToken = CancellationToken.None;
            var kafkaEvent = new KafkaEventData();
            kafkaEvent.Key = 123;
            kafkaEvent.Value = "hello world";

            mockProducer.Setup(x => x.Produce("topic", kafkaEvent));
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
        }

        [Fact]
        public async Task FlushAfterLotsOfSmallEvents()
        {
            var collector = new KafkaAsyncCollector();

            // Sending a bunch of little events
            for (int i = 0; i < 150; i++)
            {
                var e1 = new KafkaEventData() { Key = 1, Value = "hello" };
                await collector.AddAsync(e1);
            }
        }

        [Fact]
        public async Task CantSendNullEvent()
        {
            var collector = new KafkaAsyncCollector();

            await Assert.ThrowsAsync<ArgumentNullException>(
                async () => await collector.AddAsync(null));
        }
    }
}
