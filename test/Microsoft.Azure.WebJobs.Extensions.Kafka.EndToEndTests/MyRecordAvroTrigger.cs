// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class MyRecordAvroTrigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyAvroRecordTopicName, ValueType = typeof(MyAvroRecord), KeyType = typeof(string), ConsumerGroup = nameof(MyRecordAvroTrigger))] KafkaEventData[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                var myRecord = (MyAvroRecord)kafkaEvent.Value;
                log.LogInformation("{key}:{ticks}:{value}", kafkaEvent.Key, myRecord.Ticks, myRecord.ID);
            }
        }
    }
}
