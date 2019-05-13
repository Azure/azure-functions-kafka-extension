using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SampleHost
{
    public class Functions
    {
        const string Broker = "localhost:9092";
        const string StringTopicWithOnePartition = "stringTopicOnePartition";
        const string StringTopicWithTenPartitions = "stringTopicTenPartitions";

        /// <summary>
        /// Trigger for the topic 
        /// </summary>
        public void MultiItemTriggerTenPartitions(
            [KafkaTrigger(Broker, StringTopicWithTenPartitions, ConsumerGroup = "myConsumerGroup")] KafkaEventData<string>[] events,
            ILogger log)
        {
            foreach (var kafkaEvent in events)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }
    }
}
