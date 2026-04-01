// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaTargetScalerTest
    {
        const string TopicName = "topicTest";
        private readonly KafkaTargetScalerForTest<string, byte[]> targetScaler;
        private readonly Mock<IConsumer<string, byte[]>> consumer;
        private readonly Mock<KafkaMetricsProvider<string, byte[]>> metricsProvider;

        public KafkaTargetScalerTest()
        {
            consumer = new Mock<IConsumer<string, byte[]>>();
            metricsProvider = new Mock<KafkaMetricsProvider<string, byte[]>>(TopicName, new AdminClientConfig(), consumer.Object, NullLogger.Instance);

            targetScaler = new KafkaTargetScalerForTest<string, byte[]>("topicTest", "consumerGroupTest", "functionIDTest", consumer.Object, metricsProvider.Object, 1000L, NullLogger.Instance);
        }

        [Theory]
        [InlineData(null, 3, 100, 3)] // When no last scale result exists, return the current target worker count
        [InlineData(null, 3, 0, 3)]
        [InlineData(null, 10, 100, 10)]
        [InlineData(2, 3, 100, 3)] // Scaling up at any time returns the current target worker count
        [InlineData(2, 3, 10, 3)]
        [InlineData(2, 3, 0, 3)]
        [InlineData(2, 1, 61, 1)] // Scaling down after 1 minute of scale up returns the current target worker count
        [InlineData(2, 1, 180, 1)]
        [InlineData(2, 1, 1000, 1)]
        [InlineData(2, 1, 59, 2)] // Scaling down before 1 minute of scale up returns the last target worker count
        [InlineData(2, 1, 40, 2)]
        [InlineData(2, 1, 20, 2)]
        [InlineData(2, 1, 0, 2)]
        public void Throttle_As_Necessary(int? lastScaleResult, int currentTargetWorkerCount, int? lastScaleUpInSeconds, int expectedResult)
        {
            if (lastScaleResult.HasValue)
            {
                targetScaler.SetLastScalerResult(lastScaleResult ?? default);
            }
            if (lastScaleUpInSeconds.HasValue)
            {
                targetScaler.SetLastScaleUpTime(DateTime.UtcNow - TimeSpan.FromSeconds(lastScaleUpInSeconds ?? default));
            }
            var actualResult = targetScaler.ThrottleResultIfNecessary(currentTargetWorkerCount);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(null, 3, 3)]
        [InlineData(null, 5, 5)]
        [InlineData(1, 2, 1)]
        [InlineData(2, 2, 0)]
        [InlineData(3, 2, -1)]
        public void When_Last_Target_Exists_Get_Expected_Change(int? lastTargetScalerResult, int currentTargetWorkerCount, int expectedResult)
        {
            // Assign
            if (lastTargetScalerResult.HasValue)
            {
                targetScaler.SetLastScalerResult(lastTargetScalerResult ?? default);
            }
            var actualResult = targetScaler.GetChangeInWorkerCount(currentTargetWorkerCount);
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(null, 1000, 1000)]
        [InlineData(null, 100, 100)]
        [InlineData(10, 1000, 10)]
        public void When_DynamicConcurrency_Exists_Returns_DynamicConcurrency_Else_LagThreshold(int? dynamicConcurrency, long lagThreshold, int expectedResult)
        {
            // assign
            var context = new TargetScalerContext { InstanceConcurrency = dynamicConcurrency};

            // act
            var actualResult = targetScaler.GetConcurrency(context, lagThreshold);

            // assert
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(2, 1, 1)]
        [InlineData(20, 1, 1)]
        [InlineData(10, 5, 5)]
        [InlineData(201, 200, 200)]
        [InlineData(2, 5, 2)]
        [InlineData(10, 32, 10)]
        [InlineData(20, 32, 20)]
        [InlineData(201, 201, 201)]
        public void When_Target_Exceeds_Partition_Count_Returns_Partition_Count_Else_Target_Count(int targetWorkerCount, long partitionCount, int expectedResult)
        {
            var actualResult = targetScaler.ValidateWithPartitionCount(targetWorkerCount, partitionCount);

            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData(1L, 1, 2L, 2, 59)]
        [InlineData(1L, 1, 2L, 2, 30)]
        [InlineData(1L, 1, 2L, 2, 25)]
        [InlineData(1L, 1, 2L, 2, 0)]
        public async Task When_Last_Metrics_Are_Not_Old_Return_Calculated_Metrics(long lag1, int partitionCount1, long lag2, int partitionCount2, int timespanInSeconds)
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(lag1, partitionCount1);
            oldMetrics.Timestamp = DateTime.UtcNow.AddSeconds(-timespanInSeconds);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);
            var calculatedMetrics = new KafkaTriggerMetrics(lag2, partitionCount2);
            metricsProvider.Setup(x => x.GetMetricsAsync()).Returns(Task.FromResult(calculatedMetrics));
            var context = new TargetScalerContext { InstanceConcurrency = null };
            var expectedMetrics = oldMetrics;

            // Act
            var result = await targetScaler.ValidateAndGetMetrics();

            // Assert
            Assert.Equal(expectedMetrics, result);
        }

        [Theory]
        [InlineData(1L, 1, 2L, 2, 181)]
        [InlineData(1L, 1, 2L, 2, 180)]
        [InlineData(1L, 1, 2L, 2, 240)]
        [InlineData(1L, 1, 2L, 2, 1000)]
        public async Task When_Last_Metrics_Are_Old_Return_Calculated_Metrics(long lag1, int partitionCount1, long lag2, int partitionCount2, int timespanInSeconds )
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(lag1, partitionCount1);
            oldMetrics.Timestamp = DateTime.UtcNow.AddSeconds(-timespanInSeconds);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);
            var calculatedMetrics = new KafkaTriggerMetrics(lag2, partitionCount2);
            metricsProvider.Setup(x => x.GetMetricsAsync()).Returns(Task.FromResult(calculatedMetrics));
            var context = new TargetScalerContext { InstanceConcurrency = null };
            var expectedMetrics = calculatedMetrics;

            // Act
            var result = await targetScaler.ValidateAndGetMetrics();

            // Assert
            Assert.Equal(expectedMetrics, result);
        }
    }
}
