// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    internal class KafkaMetricsProviderForTest<TKey, TValue> : KafkaMetricsProvider<TKey, TValue>
    {
        List<TopicPartition> allPartitions;
        List<TopicPartition> onlyAssignedPartitions;

        public KafkaMetricsProviderForTest(string topicName, AdminClientConfig adminClientConfig, IConsumer<TKey, TValue> consumer, ILogger logger, List<TopicPartition> topicPartitions, List<TopicPartition> assignedPartitions) : base(topicName, adminClientConfig, consumer, logger)
        {
            this.allPartitions = topicPartitions;
            this.onlyAssignedPartitions = assignedPartitions;
        }

        protected override List<TopicPartition> LoadTopicPartitions()
        {
            return this.allPartitions ?? base.LoadTopicPartitions();
        }

        protected override List<TopicPartition> LoadAssignedPartitions()
        {
            return this.onlyAssignedPartitions ?? base.LoadAssignedPartitions();
        }

        internal void SetLastCalculatedMetrics(long totalLag, int partitionCount)
        {
            base.LastCalculatedMetrics = new KafkaTriggerMetrics(totalLag, partitionCount);
        }
    }
}
