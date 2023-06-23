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

        private readonly int partitionCount = 5;

        public KafkaTargetScalerTest()
        {
            consumer = new Mock<IConsumer<string, byte[]>>();
            metricsProvider = new Mock<KafkaMetricsProvider<string, byte[]>>(TopicName, new AdminClientConfig(), consumer.Object, NullLogger.Instance);

            targetScaler = new KafkaTargetScalerForTest<string, byte[]>("topicTest", "consumerGroupTest", "functionIDTest", consumer.Object, metricsProvider.Object, 1000L, NullLogger.Instance);
        }

        [Fact]
        public async Task When_Last_Metrics_Dont_Exist_Should_Return_Setup_Metric_Result()
        {
            // Assign
            var lastMetrics = new KafkaTriggerMetrics(-1L, -1);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(lastMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            var newMetrics = new KafkaTriggerMetrics(2000, partitionCount);
            metricsProvider.Setup(x => x.GetMetricsAsync()).Returns(Task.FromResult(newMetrics));

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(2, result.TargetWorkerCount);
        }

        [Fact]
        public async Task When_Last_Calculated_Metrics_Are_Older_Than_Minute_Should_Return_Setup_Metric_Result()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(2000L, partitionCount);
            oldMetrics.Timestamp = DateTime.UtcNow.AddMinutes(-2);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            var newMetrics = new KafkaTriggerMetrics(4000L, partitionCount);
            metricsProvider.Setup(x => x.GetMetricsAsync()).Returns(Task.FromResult(newMetrics));

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(4, result.TargetWorkerCount);
        }

        [Fact]
        public async Task When_Last_Calculated_Metrics_Are_Valid_Should_Return_Last_Metric_Result()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(3000L, partitionCount);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(3, result.TargetWorkerCount);
        }

        [Fact]
        public async Task When_Context_Not_Null_Should_Return_DividedByInstanceConcurrency()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(200L, partitionCount);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = 60};

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(4, result.TargetWorkerCount);
        }

        [Fact]
        public async Task When_Target_Is_Above_ParitionCount_Should_Return_PartitionCount()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(7000L, partitionCount);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(partitionCount, result.TargetWorkerCount);
        }

        [Fact]
        public async Task When_No_Last_ScaleUp_Time_Exists_For_Current_ScaleDown_Should_Return_Current_Target()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(3000L, partitionCount);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            targetScaler.SetLastScalerResult(4);

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(3, result.TargetWorkerCount);
        }

        [Fact]
        public async Task When_LastScaleUpTime_More_Than_Threshold_For_ScaleDown_Should_Return_Current_Target()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(3000L, partitionCount);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            targetScaler.SetLastScaleUpTime( DateTime.UtcNow - TimeSpan.FromMinutes(2));
            targetScaler.SetLastScalerResult(partitionCount);

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(result.TargetWorkerCount, 3);
        }

        [Fact]
        public async Task When_LastScaleUpTime_Less_Than_Threshold_For_ScaleDown_LastTarget_Exists_Should_Return_Last_Target()
        {
            // Assign
            var oldMetrics = new KafkaTriggerMetrics(3000L, partitionCount);
            metricsProvider.SetupGet(x => x.LastCalculatedMetrics).Returns(oldMetrics);

            var context = new TargetScalerContext{InstanceConcurrency = null};

            targetScaler.SetLastScaleUpTime( DateTime.UtcNow - TimeSpan.FromSeconds(30));
            targetScaler.SetLastScalerResult(partitionCount);

            // Act
            var result = await targetScaler.GetScaleResultAsync(context);

            // Assert
            Assert.Equal(partitionCount, result.TargetWorkerCount);
        }
    }
}
