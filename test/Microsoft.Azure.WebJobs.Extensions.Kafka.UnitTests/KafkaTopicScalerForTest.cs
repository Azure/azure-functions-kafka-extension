// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaTopicScalerForTest<TKey, TValue> : KafkaTopicScaler<TKey, TValue>
    {
        private List<TopicPartition> partitions;

        public KafkaTopicScalerForTest(string topic, string consumerGroup, ScaleMonitorDescriptor scaleMonitorDescriptor, IConsumer<TKey, TValue> consumer, AdminClientConfig adminClientConfig, ILogger logger)
            : base(topic, consumerGroup, scaleMonitorDescriptor, consumer, adminClientConfig, logger)
        {
        }

        public KafkaTopicScalerForTest<TKey, TValue> WithPartitions(List<TopicPartition> partitions)
        {
            this.partitions = partitions;
            return this;
        }

        protected override List<TopicPartition> LoadTopicPartitions()
        {
            return this.partitions ?? base.LoadTopicPartitions();
        }
    }
}
