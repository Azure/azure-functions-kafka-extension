using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;

namespace EventHub
{
    public class SingleKafkaTriggerQueueOutput
    {
        // KafkaTrigger sample 
        // Consume the message from "topic" on the LocalBroker.
        // For EventHubs
        [FunctionName("SingleKafkaTriggerQueueOutput")]
        [return: Queue("e2e-dotnet-single-eventhub")]
        public static string Run(
                          "e2e-kafka-dotnet-single-eventhub",
                          Username = "$ConnectionString",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string> kevent, ILogger log)
        {
            log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            return kevent.Value;
        }
    }
}
