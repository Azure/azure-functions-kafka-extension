// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Host.Scale;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    internal class KafkaTargetScalerForTest<TKey, TValue> : KafkaGenericTargetScaler<TKey, TValue>
    {
        public KafkaTargetScalerForTest(string topic, string consumerGroup, string functionID, IConsumer<TKey, TValue> consumer, KafkaMetricsProvider<TKey, TValue> metricsProvider, long lagThreshold, ILogger logger) : base(topic, consumerGroup, functionID, consumer, metricsProvider, lagThreshold, logger)
        {
        }

        public void SetLastScalerResult(int targetWorkerCount)
        {
            base.lastTargetScalerResult = new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }

        public void SetLastScaleUpTime(System.DateTime lastScaleUpTime)
        {
            base.lastScaleUpTime = lastScaleUpTime;
        }
    }
}
