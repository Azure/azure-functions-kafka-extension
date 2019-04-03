// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class OutputBindingTests
    {
        [Fact]
        public async Task AddAsync_Produce()
        {
            var mockProducer = new Mock<IKafkaProducer>();
            var cancelToken = CancellationToken.None;
            var kafkaEvent = new KafkaEventData();
            kafkaEvent.Key = 123;
            kafkaEvent.Value = "hello world";

            mockProducer.Setup(x => x.Produce("topic", kafkaEvent));
            var collection = new KafkaAsyncCollector("topic", mockProducer.Object);

            await collection.AddAsync(new KafkaEventData()
            {
                Key = 123,
                Value = "hello world"
            });
            await collection.FlushAsync();

            Assert.Equal("hello world", kafkaEvent.Value);
        }
    }
}
