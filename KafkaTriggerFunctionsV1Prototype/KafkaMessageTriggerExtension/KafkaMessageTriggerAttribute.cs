using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;

namespace KafkaMessageTriggerExtension
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public sealed class KafkaMessageTriggerAttribute : Attribute
    {     
        public string BrokerServers { get; set; } = "BrokerServers";
        public string TopicName { get; set; } = "TopicName";

        public string ConsumerGroup { get; set; } = "ConsumerGroup";
    }
}