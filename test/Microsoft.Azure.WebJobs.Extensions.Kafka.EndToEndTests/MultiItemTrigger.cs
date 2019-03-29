// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class MultiItemTrigger
    {
        public static void Trigger(
            [KafkaTrigger(Constants.Broker, Constants.StringTopicWithOnePartition, ConsumerGroup = nameof(MultiItemTrigger))] KafkaEventData[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }
    }
}
