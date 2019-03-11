using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using KafkaMessageTriggerExtension;
using Confluent.Kafka;

namespace TestFunction
{
    public static class KafkaTriggerFunction
    {
        [FunctionName("KafkaTriggerFunction")]
        public static void Run([KafkaMessageTriggerAttribute]ConsumeResult<Ignore,string> message, TraceWriter log)
        {
            log.Info($"Message:{message.Message.Value} Offset:{message.Offset.Value} Topic:{message.Topic}");
        }
    }
}
