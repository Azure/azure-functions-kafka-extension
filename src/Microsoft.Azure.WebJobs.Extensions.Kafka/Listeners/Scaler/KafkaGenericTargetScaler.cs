// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaGenericTargetScaler<Tkey, TValue> : ITargetScaler
    {
        private readonly string topicName;
        private readonly string consumerGroup;
        private readonly long lagThreshold;
        private readonly ILogger logger;
        private readonly KafkaMetricsProvider<Tkey, TValue> metricsProvider;

        protected DateTime lastScaleUpTime;
        protected TargetScalerResult lastTargetScalerResult;

        public TargetScalerDescriptor TargetScalerDescriptor { get; }

        internal KafkaGenericTargetScaler(string topic, string consumerGroup, string functionID, IConsumer<Tkey, TValue> consumer, KafkaMetricsProvider<Tkey, TValue> metricsProvider, long lagThreshold, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Invalid topic: ", nameof(topic));
            }

            if (string.IsNullOrWhiteSpace(consumerGroup))
            {
                throw new ArgumentException("Invalid consumer group: ", nameof(consumerGroup));
            }

            topicName = topic;
            this.consumerGroup = consumerGroup;
            TargetScalerDescriptor = new TargetScalerDescriptor(functionID);
            this.lagThreshold = lagThreshold;
            this.logger = logger;
            this.metricsProvider = metricsProvider;

            lastScaleUpTime = DateTime.MinValue;
            lastTargetScalerResult = null;

            this.logger.LogInformation($"Started Target Scaler - topic name: {topicName}, consumerGroup: {consumerGroup}, functionID: {functionID}, lagThreshold: {lagThreshold}.");
        }

        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            var metrics = await ValidateAndGetMetrics();
            TargetScalerResult targetScalerResult = GetScaleResultInternal(context, metrics);
            lastTargetScalerResult = targetScalerResult;
            return targetScalerResult;
        }

        internal async Task<KafkaTriggerMetrics> ValidateAndGetMetrics()
        {
            // if the metrics don't exist or the last calculated metrics
            // are older than 2 minutes, recalculate the metrics.
            var metrics = metricsProvider.LastCalculatedMetrics;
            TimeSpan metricsTimeOut = TimeSpan.FromMinutes(1);
            if (metrics == null || DateTime.UtcNow - metrics.Timestamp > metricsTimeOut)
            {
                metrics = await metricsProvider.GetMetricsAsync();
                logger.LogInformation($"Calculating metrics as last calculated don't exist or were stored 1 minute ago.");
            }
            return metrics;
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

            var targetConcurrency = GetConcurrency(context, lagThreshold);

            int targetWorkerCount = (int)Math.Ceiling(totalLag / (decimal)targetConcurrency);

            targetWorkerCount = ValidateWithPartitionCount(targetWorkerCount, partitionCount);
            targetWorkerCount = ThrottleResultIfNecessary(targetWorkerCount);
            if (GetChangeInWorkerCount(targetWorkerCount) > 0)
            {
                lastScaleUpTime = DateTime.UtcNow;
            }

            logger.LogInformation($"Target worker count for function '{TargetScalerDescriptor.FunctionId}' is '{targetWorkerCount}' (Topic='{topicName}', Total Lag ='{totalLag}', Concurrency='{targetConcurrency}', Consumer Group='{consumerGroup}', Partition Count='{partitionCount}').");

            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }

        internal int GetConcurrency(TargetScalerContext context, long lagThreshold)
        {
            // If dynamicConcurrencyEnabled is set to true, target concurrency is
            // set to instanceConcurrency value, else it is set to lagThreshold
            // (default value = 1000).
            int targetConcurrency = context.InstanceConcurrency ?? (int)lagThreshold;
            if (targetConcurrency < 1)
            {
                throw new ArgumentException("Target concurrency must be larger than 0.");
            }
            return targetConcurrency;
        }

        internal int ValidateWithPartitionCount(int targetWorkerCount, long partitionCount)
        {
            // Limit targetWorkerCount to number of partitions.
            if (targetWorkerCount > partitionCount)
            {
                targetWorkerCount = (int)partitionCount;
            }

            return targetWorkerCount;
        }

        internal int ThrottleResultIfNecessary(int targetWorkerCount)
        {
            // Throttle Scale Down if Scale Up has recently occurred.
            if (GetChangeInWorkerCount(targetWorkerCount) < 0)
            {
                var scaleDownThrottleTime = TimeSpan.FromMinutes(1);
                if (lastScaleUpTime != DateTime.MinValue && DateTime.UtcNow - lastScaleUpTime < scaleDownThrottleTime)
                {
                    if (lastTargetScalerResult != null)
                    {
                        targetWorkerCount = lastTargetScalerResult.TargetWorkerCount;
                        logger.LogInformation($"Throttling scale down as last scale up was less than 1 minute ago. Returning last target worker count: {lastTargetScalerResult.TargetWorkerCount}");
                    }
                }
            }
            return targetWorkerCount;
        }

        internal int GetChangeInWorkerCount(int targetWorkerCount)
        {
            if (lastTargetScalerResult == null)
            {
                return targetWorkerCount;
            }
            return targetWorkerCount - lastTargetScalerResult.TargetWorkerCount;
        }
    }
}