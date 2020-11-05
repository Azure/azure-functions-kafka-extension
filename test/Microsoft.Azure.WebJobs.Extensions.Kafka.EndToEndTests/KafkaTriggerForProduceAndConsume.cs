// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public class KafkaTriggerForProduceAndConsume<T> where T : IKafkaEventData
    {
        private readonly ConcurrentBag<T> testData;

        public KafkaTriggerForProduceAndConsume(ConcurrentBag<T> testData)
        {
            this.testData = testData;
        }
        public void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] T[] kafkaEvents)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {

                testData.Add(kafkaEvent);
            }
        }
    }
}