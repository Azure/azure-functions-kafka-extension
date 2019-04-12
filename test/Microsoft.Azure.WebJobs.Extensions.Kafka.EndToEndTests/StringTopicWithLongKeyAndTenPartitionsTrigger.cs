// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class StringTopicWithLongKeyAndTenPartitionsTrigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithLongKeyAndTenPartitionsName, ConsumerGroup = nameof(StringTopicWithLongKeyAndTenPartitionsTrigger), ValueType = typeof(string), KeyType = typeof(long))] KafkaEventData[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation("{key}: {value}", kafkaEvent.Key, kafkaEvent.Value.ToString());
            }
        }
    }
}
