// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{

    public class KafkaTopicScalerTest
    {
        private readonly string[] actualTopicNames = new[] { "topicTest", "topicTest2" };
        private readonly string[] configuredTopicNames = new[] { "topicTest", "^.+Test2" };

        private readonly TopicPartition partition0;
        private readonly TopicPartition partition1;
        private readonly TopicPartition partition2;
        private readonly TopicPartition partition3;
        private readonly List<TopicPartition> partitions;
        private readonly KafkaTopicScalerForTest<string, byte[]> topicScaler;
        private readonly Mock<IAdminClient> adminClient;
        private readonly Mock<IConsumer<string, byte[]>> consumer;
        private readonly Mock<AdminClientBuilder> adminClientBuilder;

        private Offset ZeroOffset => new Offset(0L);
        private TimeSpan AnyTimeSpan => It.IsAny<TimeSpan>();


        public KafkaTopicScalerTest()
        {
            consumer = new Mock<IConsumer<string, byte[]>>();
            adminClientBuilder = new Mock<AdminClientBuilder>(new AdminClientConfig());

            partition0 = new TopicPartition(actualTopicNames[0], new Partition(0));
            partition1 = new TopicPartition(actualTopicNames[0], new Partition(1));
            partition2 = new TopicPartition(actualTopicNames[1], new Partition(2));
            partition3 = new TopicPartition(actualTopicNames[1], new Partition(3));

            partitions = new List<TopicPartition>
            { 
                partition0,
                partition1,
                partition2,
                partition3
            };

            topicScaler = new KafkaTopicScalerForTest<string, byte[]>(
                configuredTopicNames,
                "consumer-group-test",
                "testfunction",
                consumer.Object, adminClientBuilder.Object,
                NullLogger.Instance);

            adminClient = new Mock<IAdminClient>();

            adminClientBuilder.Setup(x => x.Build()).Returns(adminClient.Object);

            adminClient.Setup(x => x.GetMetadata(It.IsAny<TimeSpan>())).Returns(new Metadata(null, partitions.GroupBy(p=>p.Topic).Select(x=>
                new TopicMetadata(
                    x.Key, 
                    new List<PartitionMetadata>(x.Select(t=>new PartitionMetadata(t.Partition.Value, 0, null, null, null))),
                    null
                )
            ).ToList(), 0, ""));

            adminClient.Setup(x => x.GetMetadata(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns((string topic, TimeSpan timeOut)=> new Metadata(null, partitions.Where(p => p.Topic == topic).GroupBy(p => p.Topic).Select(x =>
                new TopicMetadata(
                    x.Key,
                    new List<PartitionMetadata>(x.Select(t => new PartitionMetadata(t.Partition.Value, 0, null, null, null))),
                    null
                )
            ).ToList(), 0, ""));
        }

        
        [Fact]
        public void ScaleMonitor_Id_ReturnsExpectedValue()
        {
            Assert.Equal("testfunction-kafkatrigger-topictest-^.+test2-consumer-group-test", topicScaler.Descriptor.Id);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task When_Offset_Is_Zero_Should_Return_No_Lag(bool mockAdminClient)
        {
            if (!mockAdminClient)
            {
                topicScaler.WithPartitions(partitions);
            }
            consumer.Setup(x => x.Committed(It.IsNotNull<IEnumerable<TopicPartition>>(), AnyTimeSpan))
                .Returns(new List<TopicPartitionOffset>
                {
                    new TopicPartitionOffset(partition0, ZeroOffset),
                    new TopicPartitionOffset(partition1, ZeroOffset),
                    new TopicPartitionOffset(partition2, ZeroOffset),
                    new TopicPartitionOffset(partition3, ZeroOffset),
                });

            consumer.Setup(x => x.QueryWatermarkOffsets(It.IsAny<TopicPartition>(), AnyTimeSpan))
                .Returns(new WatermarkOffsets(ZeroOffset, ZeroOffset));
            

            var metrics = await topicScaler.GetMetricsAsync();
            Assert.Equal(partitions.Count, metrics.PartitionCount);
            Assert.Equal(0, metrics.TotalLag);
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task When_Committed_Is_Behind_Offset_Should_Return_Combined_Lag(bool mockAdminClient)
        {
            if (!mockAdminClient)
            {
                topicScaler.WithPartitions(partitions);
            }
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

            consumer.Setup(x => x.QueryWatermarkOffsets(It.IsAny<TopicPartition>(), AnyTimeSpan))
                .Returns(new WatermarkOffsets(currentOffset, currentOffset));

            var metrics = await topicScaler.GetMetricsAsync();
            Assert.Equal(partitions.Count, metrics.PartitionCount);
            var diff1 = currentOffset - largestLagOffset;
            var diff2 = currentOffset - minimalLagOffset;
            Assert.Equal(diff1 + diff2, metrics.TotalLag);
        }

        [Fact]
        public void When_No_Lag_Is_Found_Should_Vote_Scale_Down()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(0, partitions.Count),
                    new KafkaTriggerMetrics(0, partitions.Count),
                    new KafkaTriggerMetrics(0, partitions.Count),
                    new KafkaTriggerMetrics(0, partitions.Count),
                    new KafkaTriggerMetrics(0, partitions.Count),
                },
                WorkerCount = 1,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleIn, result.Vote);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void When_Not_Enough_Metrics_Are_Available_Should_Vote_None(int metricCount)
        {
            var metrics = new List<KafkaTriggerMetrics>();
            for (int i = 0; i < metricCount; i++)
            {
                metrics.Add(new KafkaTriggerMetrics(i, partitions.Count));
            }

            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = metrics.ToArray(),
                WorkerCount = 1,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.None, result.Vote);
        }


        [Fact]
        public void When_WorkerCount_More_PartitionCount_Should_Vote_Scale_Down()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(5, partitions.Count),
                    new KafkaTriggerMetrics(6, partitions.Count),
                    new KafkaTriggerMetrics(76, partitions.Count),
                    new KafkaTriggerMetrics(22, partitions.Count),
                    new KafkaTriggerMetrics(11, partitions.Count),
                },
                WorkerCount = partitions.Count + 1,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleIn, result.Vote);
        }

        [Fact]
        public void When_LagIncreasing_Should_Vote_Scale_Up()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(10, partitions.Count),
                    new KafkaTriggerMetrics(212, partitions.Count),
                    new KafkaTriggerMetrics(32323, partitions.Count),
                    new KafkaTriggerMetrics(121222, partitions.Count),
                    new KafkaTriggerMetrics(123456, partitions.Count),
                },
                WorkerCount = 1,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleOut, result.Vote);
        }

        [Fact]
        public void When_LagDecreasing_Slowly_Should_Vote_None()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(10_005, partitions.Count),
                    new KafkaTriggerMetrics(10_004, partitions.Count),
                    new KafkaTriggerMetrics(10_003, partitions.Count),
                    new KafkaTriggerMetrics(10_002, partitions.Count),
                    new KafkaTriggerMetrics(10_001, partitions.Count),
                },
                WorkerCount = partitions.Count,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.None, result.Vote);
        }

        [Fact]
        public void When_LagDecreasing_Slowly_At_Threshold_Should_Vote_None()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(4_000, partitions.Count),
                    new KafkaTriggerMetrics(3_999, partitions.Count),
                    new KafkaTriggerMetrics(3_998, partitions.Count),
                    new KafkaTriggerMetrics(3_997, partitions.Count),
                    new KafkaTriggerMetrics(3_996, partitions.Count),
                },
                WorkerCount = partitions.Count,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.None, result.Vote);
        }

        [Fact]
        public void When_LagDecreasing_Slowly_Under_Threshold_Should_Vote_ScaleIn()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(4_000, partitions.Count),
                    new KafkaTriggerMetrics(3_999, partitions.Count),
                    new KafkaTriggerMetrics(3_998, partitions.Count),
                    new KafkaTriggerMetrics(3_997, partitions.Count),
                    new KafkaTriggerMetrics(2_999, partitions.Count),
                },
                WorkerCount = partitions.Count,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleIn, result.Vote);
        }

        [Fact]
        public void When_LagIncreasing_Last_Lag_Less_LagThreshold_Should_Vote_Scale_Out()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(10, partitions.Count),
                    new KafkaTriggerMetrics(212, partitions.Count),
                    new KafkaTriggerMetrics(333, partitions.Count),
                    new KafkaTriggerMetrics(1122, partitions.Count),
                    new KafkaTriggerMetrics(1356, partitions.Count),
                },
                WorkerCount = 1,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleOut, result.Vote);
        }

        [Fact]
        public void When_LagIncreasing_Equal_PartionCount_Should_Vote_Scale_None()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(10, partitions.Count),
                    new KafkaTriggerMetrics(212, partitions.Count),
                    new KafkaTriggerMetrics(32323, partitions.Count),
                    new KafkaTriggerMetrics(121222, partitions.Count),
                    new KafkaTriggerMetrics(123456, partitions.Count),
                },
                WorkerCount = partitions.Count,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.None, result.Vote);
        }

        [Fact]
        public void When_LagDecreasing_Should_Vote_Scale_In()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(10, partitions.Count),
                    new KafkaTriggerMetrics(9, partitions.Count),
                    new KafkaTriggerMetrics(8, partitions.Count),
                    new KafkaTriggerMetrics(7, partitions.Count),
                    new KafkaTriggerMetrics(6, partitions.Count),
                },
                WorkerCount = partitions.Count,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleIn, result.Vote);
        }
    }
}
