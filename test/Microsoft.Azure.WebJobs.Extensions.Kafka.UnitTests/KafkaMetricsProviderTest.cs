// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
//using Castle.Core.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaMetricsProviderTest
    {
        const string TopicName = "topicTest";
        private List<TopicPartition> topicPartitions;
        private List<TopicPartition> assignedPartitions;

        private readonly TopicPartition partition0;
        private readonly TopicPartition partition1;
        private readonly TopicPartition partition2;
        private readonly TopicPartition partition3;

        private readonly Mock<IConsumer<string, byte[]>> consumer;
        private readonly KafkaMetricsProviderForTest<string, byte[]> metricsProvider;

        private Offset ZeroOffset => new Offset(0L);
        private TimeSpan AnyTimeSpan => It.IsAny<TimeSpan>();

        public KafkaMetricsProviderTest()
        {
            partition0 = new TopicPartition(TopicName, new Partition(0));
            partition1 = new TopicPartition(TopicName, new Partition(1));
            partition2 = new TopicPartition(TopicName, new Partition(2));
            partition3 = new TopicPartition(TopicName, new Partition(3));

            this.topicPartitions = new List<TopicPartition>
            {
                partition0,
                partition1,
                partition2,
                partition3
            };

            this.assignedPartitions = new List<TopicPartition>
            {
                partition0,
                partition1 
            };

            consumer = new Mock<IConsumer<string, byte[]>>();
            metricsProvider = new KafkaMetricsProviderForTest<string, byte[]>(TopicName, new AdminClientConfig(), consumer.Object, NullLogger.Instance, topicPartitions, assignedPartitions);
        }


        [Fact]
        public async Task When_Offset_Is_Zero_Should_Return_No_Lag()
        {
            consumer.Setup(x => x.Committed(It.IsNotNull<IEnumerable<TopicPartition>>(), AnyTimeSpan))
            .Returns(new List<TopicPartitionOffset>
            {
                new TopicPartitionOffset(partition0, ZeroOffset),
                new TopicPartitionOffset(partition1, ZeroOffset),
                new TopicPartitionOffset(partition2, ZeroOffset),
                new TopicPartitionOffset(partition3, ZeroOffset),
                });

            consumer.Setup(x => x.GetWatermarkOffsets(It.IsIn(partition0, partition1)))
                .Returns(new WatermarkOffsets(ZeroOffset, ZeroOffset));
            consumer.Setup(x => x.QueryWatermarkOffsets(It.IsIn(partition2, partition3), AnyTimeSpan))
                .Returns(new WatermarkOffsets(ZeroOffset, ZeroOffset));

            var metrics = await metricsProvider.GetMetricsAsync();
            Assert.Equal(topicPartitions.Count, metrics.PartitionCount);
            Assert.Equal(0, metrics.TotalLag);
        }

        [Fact]
        public async Task When_Committed_Is_Behind_Offset_Should_Return_Combined_Lag()
        {
            const long currentOffset = 100;
            const long largestLagOffset = currentOffset - 50;
            const long minimalLagOffset = currentOffset - 1;

            consumer.Setup(x => x.Committed(It.IsNotNull<IEnumerable<TopicPartition>>(), AnyTimeSpan))
                .Returns(new List<TopicPartitionOffset>
                {
                    new TopicPartitionOffset(partition0, minimalLagOffset),
                    new TopicPartitionOffset(partition1, currentOffset),
                    new TopicPartitionOffset(partition2, largestLagOffset),
                    new TopicPartitionOffset(partition3, currentOffset),
                });

            consumer.Setup(x => x.GetWatermarkOffsets(It.IsIn(partition0, partition1)))
                .Returns(new WatermarkOffsets(currentOffset, currentOffset));
            consumer.Setup(x => x.QueryWatermarkOffsets(It.IsIn(partition2, partition3), AnyTimeSpan))
                .Returns(new WatermarkOffsets(currentOffset, currentOffset));

            var metrics = await metricsProvider.GetMetricsAsync();
            Assert.Equal(topicPartitions.Count, metrics.PartitionCount);
            var diff1 = currentOffset - largestLagOffset;
            var diff2 = currentOffset - minimalLagOffset;
            Assert.Equal(diff1 + diff2, metrics.TotalLag);
        }
    }
}
