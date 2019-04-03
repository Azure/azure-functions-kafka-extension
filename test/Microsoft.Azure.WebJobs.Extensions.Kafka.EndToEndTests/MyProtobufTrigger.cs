// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class MyProtobufTrigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyProtobufTopicName, ValueType = typeof(ProtoUser), KeyType = typeof(string), ConsumerGroup = nameof(MyRecordAvroTrigger))] KafkaEventData[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                var user = (ProtoUser)kafkaEvent.Value;
                log.LogInformation("{key}:{favoriteColor}:{name}", kafkaEvent.Key, user.FavoriteColor, user.Name);
            }
        }
    }
}
