using Confluent.Kafka;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaEventData
    {
        public object Key { get; set; }
        public long Offset { get; set; }
        public int Partition { get; set; }
        public string Topic { get; set; }
        public DateTime Timestamp { get; set; }
        public object Value { get; set; }

        public KafkaEventData(byte[] bytes)
        {
        }

        public KafkaEventData(IConsumeResultData msg)
        {
            this.Key = msg.Key;
            this.Offset = msg.Offset;
            this.Partition = msg.Partition;
            this.Topic = msg.Topic;
            this.Timestamp = msg.Timestamp;
            this.Value = msg.Value;
        }
    }
}