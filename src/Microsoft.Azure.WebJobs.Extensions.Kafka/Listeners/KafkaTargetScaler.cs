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
        //private readonly AdminClientConfig adminClientConfig;
        //private readonly IConsumer<Tkey, TValue> consumer;
        private readonly long lagThreshold;
        private readonly int maxBatchSize;
        private readonly ILogger logger;
        private readonly KafkaMetricsProvider<Tkey, TValue> kafkaMetricsProvider;

        public TargetScalerDescriptor TargetScalerDescriptor { get; }
 
        public KafkaTargetScaler(string topic, string consumerGroup, string functionID, IConsumer<Tkey, TValue> consumer, AdminClientConfig adminClientConfig, long lagThreshold, int maxBatchSize, ILogger logger)
        {
            // also get the target unprocessed event threshold per worker
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Invalid topic: ", nameof(topic));
            } 

            if (string.IsNullOrWhiteSpace(consumerGroup))
            {
                throw new ArgumentException("Invalid consumer group", nameof(consumerGroup));
            }

            this.topicName = topic;
            this.consumerGroup = consumerGroup;
            // ----> check the format of targetscalerdescriptor once.
            this.TargetScalerDescriptor = new TargetScalerDescriptor(functionID);
            this.lagThreshold = lagThreshold;
            this.maxBatchSize = maxBatchSize;
            //this.consumer = consumer;
            //this.adminClientConfig = adminClientConfig ?? throw new ArgumentNullException(nameof(adminClientConfig));
            this.logger = logger;
            this.kafkaMetricsProvider = new KafkaMetricsProvider<Tkey, TValue>(topicName, adminClientConfig, consumer, logger);
            this.logger.LogInformation($"Started Target scaler - topic name: {topicName}, consumerGroup {consumerGroup}, functionID: {functionID}, lagThreshold: {lagThreshold}, maxBatchSize: {maxBatchSize}.");
        }

        // defining ITargetScaler interface method - GetScaleResultAsync: returns TargetScalerResult {TargetWorkerCount;}.
        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            KafkaTriggerMetrics metrics = await kafkaMetricsProvider.GetMetricsAsync();

            return GetScaleResultInternal(context, metrics);
        }

        internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, KafkaTriggerMetrics metrics)
        {
            var eventCount = metrics.TotalLag;
            var targetConcurrency = context.InstanceConcurrency ?? this.lagThreshold;
            if (context.InstanceConcurrency.HasValue)
            {
                this.logger.LogInformation($"Dynamic conurrency: {context.InstanceConcurrency}");
            }  
            else
            {
                this.logger.LogInformation($"Static concurrency: {this.lagThreshold}");
            }

            if (targetConcurrency < 1)
            {
                throw new ArgumentException("Target concurrency must be larger than 0.");
            }

            int targetWorkerCount = (int) Math.Ceiling(eventCount / (decimal) targetConcurrency);
            if (targetWorkerCount > metrics.PartitionCount)
            {
                targetWorkerCount = (int) metrics.PartitionCount;
            }

            this.logger.LogInformation($"TargetWorkerCount: {targetWorkerCount}. For the topic {this.topicName}, consumer group {consumerGroup}.");   

            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }
    }
}