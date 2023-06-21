// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    class KafkaTargetScaler<Tkey, TValue> : ITargetScaler
    {
        // Initialise variables required.
        private readonly string topicName;
        private readonly string consumerGroup;
        private readonly long lagThreshold;
        private readonly ILogger logger;
        private readonly KafkaMetricsProvider<Tkey, TValue> metricsProvider;

        private DateTime lastScaleUpTime;
        private TargetScalerResult lastTargetScalerResult;

        public TargetScalerDescriptor TargetScalerDescriptor { get; }
 
        internal KafkaTargetScaler(string topic, string consumerGroup, string functionID, IConsumer<Tkey, TValue> consumer, KafkaMetricsProvider<Tkey, TValue> metricsProvider, long lagThreshold, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Invalid topic: ", nameof(topic));
            } 

            if (string.IsNullOrWhiteSpace(consumerGroup))
            {
                throw new ArgumentException("Invalid consumer group: ", nameof(consumerGroup));
            }

            this.topicName = topic;
            this.consumerGroup = consumerGroup;
            this.TargetScalerDescriptor = new TargetScalerDescriptor(functionID);
            this.lagThreshold = lagThreshold;
            this.logger = logger;
            this.metricsProvider = metricsProvider;

            this.lastScaleUpTime = DateTime.MinValue;
            this.lastTargetScalerResult = new TargetScalerResult();

            this.logger.LogInformation($"Started Target Scaler - topic name: {topicName}, consumerGroup: {consumerGroup}, functionID: {functionID}, lagThreshold: {lagThreshold}.");
        }

        // defining ITargetScaler interface method - GetScaleResultAsync: returns TargetScalerResult {TargetWorkerCount;}.
        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            KafkaTriggerMetrics metrics = await metricsProvider.GetMetricsAsync();

            return GetScaleResultInternal(context, metrics);
        }

        internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, KafkaTriggerMetrics metrics)
        {
            var totalLag = metrics.TotalLag;
            var partitionCount = metrics.PartitionCount;

            // Since Kafka Extension supports only Premium plan for run time based scaling,
            // the targetWorkerCount is set to 1 even when the totalLag is 0.
            // This can be changed to 0 when the extension supports all plans.
            if (totalLag == 0)
            {
                return new TargetScalerResult
                {
                    TargetWorkerCount = 1
                };
            }

            var targetConcurrency = context.InstanceConcurrency ?? this.lagThreshold;

            if (targetConcurrency < 1)
            {
                throw new ArgumentException("Target concurrency must be larger than 0.");
            }

            int targetWorkerCount = (int) Math.Ceiling(totalLag / (decimal) targetConcurrency);

            targetWorkerCount = ValidateWithPartitionCount(targetWorkerCount, partitionCount);
            targetWorkerCount = ThrottleResultIfNecessary(targetWorkerCount);
            if (GetChangeInWorkerCount(targetWorkerCount) > 0)
            {
                this.lastScaleUpTime = DateTime.UtcNow;
            }

            this.logger.LogInformation($"Total Lag: {totalLag}, concurrency: {targetConcurrency} TargetWorkerCount: {targetWorkerCount}. For the topic {this.topicName}, consumer group {consumerGroup}.");   

            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }

        internal int ValidateWithPartitionCount(int targetWorkerCount, long partitionCount)
        {
            if (targetWorkerCount > partitionCount)
            {
                targetWorkerCount = (int) partitionCount;
            }

            return targetWorkerCount;
        }

        internal int ThrottleResultIfNecessary(int targetWorkerCount)
        {
            if (GetChangeInWorkerCount(targetWorkerCount) < 0 && DateTime.UtcNow - this.lastScaleUpTime < TimeSpan.FromMinutes(1))
            {
                targetWorkerCount = this.lastTargetScalerResult.TargetWorkerCount;
                this.logger.LogInformation("Throttling scale down as last scale up was less than 1 minute ago.");
            }
            return targetWorkerCount;
        }

        internal int GetChangeInWorkerCount(int targetWorkerCount)
        {
            return targetWorkerCount - lastTargetScalerResult.TargetWorkerCount;
        }
    }
}