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
        private readonly int? unprocessedEventThreshold; 
        private readonly ILogger logger;
        private readonly KafkaMetricsProvider<Tkey, TValue> kafkaMetricsProvider;

        public TargetScalerDescriptor TargetScalerDescriptor { get; }
 
        public KafkaTargetScaler(string topic, string consumerGroup, string functionID, IConsumer<Tkey, TValue> consumer, AdminClientConfig adminClientConfig, int? unprocessedEventThreshold, ILogger logger)
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
            this.unprocessedEventThreshold = unprocessedEventThreshold;
            //this.consumer = consumer;
            //this.adminClientConfig = adminClientConfig ?? throw new ArgumentNullException(nameof(adminClientConfig));
            this.logger = logger;
            this.kafkaMetricsProvider = new KafkaMetricsProvider<Tkey, TValue>(topicName, adminClientConfig, consumer, logger);
        }

        // defining ITargetScaler interface method - GetScaleResultAsync: returns TargetScalerResult {TargetWorkerCount;}.
        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            var metrics = await kafkaMetricsProvider.GetMetricsAsync();
            long eventCount = metrics.EventCount;

            return GetScaleResultInternal(context, eventCount);
        }

        internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, long eventCount)
        {
            var targetConcurrency = context.InstanceConcurrency ?? unprocessedEventThreshold;
            if (targetConcurrency < 1)
            {
                throw new ArgumentException("Target concurrency must be larger than 0.");
            }
            int targetWorkerCount = (int) Math.Ceiling(eventCount / (decimal) targetConcurrency);

            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }
    }
}