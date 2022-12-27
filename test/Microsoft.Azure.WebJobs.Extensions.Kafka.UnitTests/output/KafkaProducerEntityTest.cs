// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Moq;
using System;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.output
{
    public class KafkaProducerEntityTest
    {
        private Mock<IKafkaProducerFactory> kafkaProducerFactory = new Mock<IKafkaProducerFactory>();
        private Mock<IKafkaProducer> kafkaProducer = new Mock<IKafkaProducer>();

        [Fact]
        public void SendAndCreateEntityIfNotExistsAsync_single_event_string()
        {
            KafkaProducerEntity kafkaProducerEntity = new KafkaProducerEntity();
            kafkaProducerEntity.KafkaProducerFactory = kafkaProducerFactory.Object;
            kafkaProducerFactory.Setup(e => e.Create(It.IsAny<KafkaProducerEntity>())).Returns(kafkaProducer.Object);
            IKafkaProducer kafkaProducerObj = kafkaProducer.Object;
            kafkaProducer.Setup(e => e.Produce(It.IsAny<string>(), null));
            KafkaEventData<string> eventData = new KafkaEventData<string>();
            eventData.Value = "test1";
            Task task = kafkaProducerEntity.SendAndCreateEntityIfNotExistsAsync<object>(eventData, Guid.NewGuid(), CancellationToken.None);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public void SendAndCreateEntityIfNotExistsAsync_multiple_events_string()
        {
            KafkaProducerEntity kafkaProducerEntity = new KafkaProducerEntity();
            kafkaProducerEntity.KafkaProducerFactory = kafkaProducerFactory.Object;
            kafkaProducerFactory.Setup(e => e.Create(It.IsAny<KafkaProducerEntity>())).Returns(kafkaProducer.Object);
            IKafkaProducer kafkaProducerObj = kafkaProducer.Object;
            kafkaProducer.Setup(e => e.Produce(It.IsAny<string>(), null));
            List<KafkaEventData<string>> eventList = new List<KafkaEventData<string>>();
            for (int i = 0; i < 20; i++)
            {
                KafkaEventData<string> eventData = new KafkaEventData<string>();
                eventData.Value = "testVal";
                eventList.Add(eventData);
            }
            Task task = kafkaProducerEntity.SendAndCreateEntityIfNotExistsAsync<object>(eventList, Guid.NewGuid(), CancellationToken.None);
            Assert.True(task.IsCompleted);
        }
    }
}
