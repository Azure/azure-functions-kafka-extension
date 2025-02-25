// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{

    public class KafkaTopicScalerTest
    {
        const string TopicName = "topicTest";

        private readonly int topicPartitionCount;
        private readonly KafkaGenericTopicScaler<string, byte[]> topicScaler;
        private readonly Mock<IConsumer<string, byte[]>> consumer;
        private readonly Mock<KafkaMetricsProvider<string, byte[]>> metricsProvider;

        public KafkaTopicScalerTest()
        {
            consumer = new Mock<IConsumer<string, byte[]>>();
            metricsProvider = new Mock<KafkaMetricsProvider<string, byte[]>>(TopicName, new AdminClientConfig(), consumer.Object, NullLogger.Instance);

            topicScaler = new KafkaGenericTopicScaler<string, byte[]>(
                TopicName,
                "consumer-group-test",
                "testfunction",
                consumer.Object, metricsProvider.Object,
                1000L,
                NullLogger.Instance);

            this.topicPartitionCount = 4; 
        }

        [Fact]
        public void ScaleMonitor_Id_ReturnsExpectedValue()
        {
            Assert.Equal("testfunction-kafkatrigger-topictest-consumer-group-test", topicScaler.Descriptor.Id);
        }

        [Fact]
        public void ScaleMonitor_FunctionId_ReturnsExpectedValue()
        {
            Assert.Equal("testfunction", topicScaler.Descriptor.FunctionId);
        }

        [Fact]
        public void When_No_Lag_Is_Found_Should_Vote_Scale_Down()
        {
            var context = new ScaleStatusContext<KafkaTriggerMetrics>()
            {
                Metrics = new KafkaTriggerMetrics[]
                {
                    new KafkaTriggerMetrics(0, topicPartitionCount),
                    new KafkaTriggerMetrics(0, topicPartitionCount),
                    new KafkaTriggerMetrics(0, topicPartitionCount),
                    new KafkaTriggerMetrics(0, topicPartitionCount),
                    new KafkaTriggerMetrics(0, topicPartitionCount),
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
                metrics.Add(new KafkaTriggerMetrics(i, topicPartitionCount));
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
                    new KafkaTriggerMetrics(5, topicPartitionCount),
                    new KafkaTriggerMetrics(6, topicPartitionCount),
                    new KafkaTriggerMetrics(76, topicPartitionCount),
                    new KafkaTriggerMetrics(22, topicPartitionCount),
                    new KafkaTriggerMetrics(11, topicPartitionCount),
                },
                WorkerCount = topicPartitionCount + 1,
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
                    new KafkaTriggerMetrics(10, topicPartitionCount),
                    new KafkaTriggerMetrics(212, topicPartitionCount),
                    new KafkaTriggerMetrics(32323, topicPartitionCount),
                    new KafkaTriggerMetrics(121222, topicPartitionCount),
                    new KafkaTriggerMetrics(123456, topicPartitionCount),
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
                    new KafkaTriggerMetrics(10_005, topicPartitionCount),
                    new KafkaTriggerMetrics(10_004, topicPartitionCount),
                    new KafkaTriggerMetrics(10_003, topicPartitionCount),
                    new KafkaTriggerMetrics(10_002, topicPartitionCount),
                    new KafkaTriggerMetrics(10_001, topicPartitionCount),
                },
                WorkerCount = topicPartitionCount,
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
                    new KafkaTriggerMetrics(4_000, topicPartitionCount),
                    new KafkaTriggerMetrics(3_999, topicPartitionCount),
                    new KafkaTriggerMetrics(3_998, topicPartitionCount),
                    new KafkaTriggerMetrics(3_997, topicPartitionCount),
                    new KafkaTriggerMetrics(3_996, topicPartitionCount),
                },
                WorkerCount = topicPartitionCount,
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
                    new KafkaTriggerMetrics(4_000, topicPartitionCount),
                    new KafkaTriggerMetrics(3_999, topicPartitionCount),
                    new KafkaTriggerMetrics(3_998, topicPartitionCount),
                    new KafkaTriggerMetrics(3_997, topicPartitionCount),
                    new KafkaTriggerMetrics(2_999, topicPartitionCount),
                },
                WorkerCount = topicPartitionCount,
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
                    new KafkaTriggerMetrics(10, topicPartitionCount),
                    new KafkaTriggerMetrics(212, topicPartitionCount),
                    new KafkaTriggerMetrics(333, topicPartitionCount),
                    new KafkaTriggerMetrics(1122, topicPartitionCount),
                    new KafkaTriggerMetrics(1356, topicPartitionCount),
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
                    new KafkaTriggerMetrics(10, topicPartitionCount),
                    new KafkaTriggerMetrics(212, topicPartitionCount),
                    new KafkaTriggerMetrics(32323, topicPartitionCount),
                    new KafkaTriggerMetrics(121222, topicPartitionCount),
                    new KafkaTriggerMetrics(123456, topicPartitionCount),
                },
                WorkerCount = topicPartitionCount,
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
                    new KafkaTriggerMetrics(10, topicPartitionCount),
                    new KafkaTriggerMetrics(9, topicPartitionCount),
                    new KafkaTriggerMetrics(8, topicPartitionCount),
                    new KafkaTriggerMetrics(7, topicPartitionCount),
                    new KafkaTriggerMetrics(6, topicPartitionCount),
                },
                WorkerCount = topicPartitionCount,
            };

            var result = topicScaler.GetScaleStatus(context);
            Assert.NotNull(result);
            Assert.Equal(ScaleVote.ScaleIn, result.Vote);
        }
    }
}
