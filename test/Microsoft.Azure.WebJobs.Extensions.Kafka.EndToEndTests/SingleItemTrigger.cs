// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class SingleItemTrigger
    {
        public static void Trigger(
            [KafkaTrigger(Constants.Broker, Constants.StringTopicWithOnePartition, ConsumerGroup = nameof(SingleItemTrigger))] KafkaEventData kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
        }
    }
}
