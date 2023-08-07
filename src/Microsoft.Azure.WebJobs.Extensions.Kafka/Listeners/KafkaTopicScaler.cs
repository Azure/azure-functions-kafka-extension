// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaTopicScaler<TKey, TValue> : IScaleMonitor<KafkaTriggerMetrics>
    {
        private readonly string topicName;
        private readonly string consumerGroup;
        private readonly ILogger logger;
        private readonly long lagThreshold;
        private readonly KafkaMetricsProvider<TKey, TValue> metricsProvider;

        public ScaleMonitorDescriptor Descriptor { get; }

        internal KafkaTopicScaler(string topic, string consumerGroup, string functionId, IConsumer<TKey, TValue> consumer, KafkaMetricsProvider<TKey, TValue> metricsProvider, long lagThreshold, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Invalid topic", nameof(topic));
            }

            if (string.IsNullOrWhiteSpace(consumerGroup))
            {
                throw new ArgumentException("Invalid consumer group", nameof(consumerGroup));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.topicName = topic;
            this.Descriptor = new ScaleMonitorDescriptor($"{functionId}-kafkatrigger-{topicName}-{consumerGroup}".ToLower(), functionId);
            this.consumerGroup = consumerGroup;
            this.lagThreshold = lagThreshold;
            this.metricsProvider = metricsProvider;
            this.logger.LogInformation($"Started Topic scaler - topic name: {topicName}, consumerGroup {consumerGroup}, functionID: {functionId}, lagThreshold: {lagThreshold}.");
        }

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await this.metricsProvider.GetMetricsAsync();
        }

        public Task<KafkaTriggerMetrics> GetMetricsAsync()
        {
            return Task.Run(() => this.metricsProvider.GetMetricsAsync());
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.OfType<KafkaTriggerMetrics>().ToArray());
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<KafkaTriggerMetrics> context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.ToArray());
        }

        private ScaleStatus GetScaleStatusCore(int workerCount, KafkaTriggerMetrics[] metrics)
        {
            var status = new ScaleStatus
            {
                Vote = ScaleVote.None,
            };

            const int NumberOfSamplesToConsider = 5;

            // At least 5 samples are required to make a scale decision for the rest of the checks.
            if (metrics == null || metrics.Length < NumberOfSamplesToConsider)
            {
                return status;
            }

            var lastMetrics = metrics.Last();
            long totalLag = lastMetrics.TotalLag;
            long partitionCount = lastMetrics.PartitionCount;
            long lagThreshold = this.lagThreshold;

            // We shouldn't assign more workers than there are partitions
            // This check is first, because it is independent of load or number of samples.
            if (partitionCount > 0 && partitionCount < workerCount)
            {
                status.Vote = ScaleVote.ScaleIn;

                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation($"Number of instances ({workerCount}) is too high relative to number of partitions ({partitionCount}). For topic {this.topicName}, for consumer group {this.consumerGroup}.");
                }

                return status;
            }

            // Check to see if the Kafka consumer has been empty for a while. Only if all metrics samples are empty do we scale down.
            bool partitionIsIdle = metrics.All(p => p.TotalLag == 0);

            if (partitionIsIdle)
            {
                status.Vote = ScaleVote.ScaleIn;
                if (this.logger.IsEnabled(LogLevel.Information))
                {
                    this.logger.LogInformation($"Topic '{this.topicName}', for consumer group {this.consumerGroup}' is idle.");
                }

                return status;
            }

            // Maintain a minimum ratio of 1 worker per lagThreshold --1,000 unprocessed message.
            if (totalLag > workerCount * lagThreshold)
            {
                if (workerCount < partitionCount)
                { 
                    status.Vote = ScaleVote.ScaleOut;

                    if (this.logger.IsEnabled(LogLevel.Information))
                    {
                        this.logger.LogInformation($"Total lag ({totalLag}) is less than the number of instances ({workerCount}). Scale out, for topic {this.topicName}, for consumer group {this.consumerGroup}.");
                    }
                }
                return status;
            }
            
            // Samples are in chronological order. Check for a continuous increase in unprocessed message count.
            // If detected, this results in an automatic scale out for the site container.
            if (metrics[0].TotalLag > 0)
            {
                if (workerCount < partitionCount)
                {
                    bool queueLengthIncreasing = IsTrueForLast(
                        metrics,
                        NumberOfSamplesToConsider,
                        (prev, next) => prev.TotalLag < next.TotalLag) && metrics[0].TotalLag > 0;

                    if (queueLengthIncreasing)
                    {
                        status.Vote = ScaleVote.ScaleOut;

                        if (this.logger.IsEnabled(LogLevel.Information))
                        {
                            this.logger.LogInformation($"Total lag ({totalLag}) is less than the number of instances ({workerCount}). Scale out, for topic {this.topicName}, for consumer group {this.consumerGroup}.");
                        }
                        return status;
                    }
                }
            }
            
            if (workerCount > 1)
            {
                bool queueLengthDecreasing = IsTrueForLast(
                    metrics,
                    NumberOfSamplesToConsider,
                    (prev, next) => prev.TotalLag > next.TotalLag);

                if (queueLengthDecreasing)
                {
                    // Only vote down if the new workerCount / totalLag < threshold
                    // Example: 4 workers, only scale in if totalLag <= 2999 (3000 < (3 * 1000))
                    var proposedWorkerCount = workerCount - 1;
                    var proposedLagPerWorker = totalLag / proposedWorkerCount;
                    if (proposedLagPerWorker < lagThreshold)
                    {
                        status.Vote = ScaleVote.ScaleIn;

                        if (this.logger.IsEnabled(LogLevel.Information))
                        {
                            this.logger.LogInformation($"Total lag length is decreasing for topic {this.topicName}, for consumer group {this.consumerGroup}.");
                        }                    
                    }                
                }
            }
              
            return status;
        }

        private static bool IsTrueForLast(IList<KafkaTriggerMetrics> samples, int count, Func<KafkaTriggerMetrics, KafkaTriggerMetrics, bool> predicate)
        {
            if (samples.Count < count)
            {
                return false;
            }

            // Walks through the list from left to right starting at len(samples) - count.
            for (int i = samples.Count - count; i < samples.Count - 1; i++)
            {
                if (!predicate(samples[i], samples[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}