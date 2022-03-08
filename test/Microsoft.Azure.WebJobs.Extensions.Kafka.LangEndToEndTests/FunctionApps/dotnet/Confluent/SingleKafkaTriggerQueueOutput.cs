using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;

namespace KafkaFunctionApp
{
    public class SingleKafkaTriggerQueueOutput
    {
        // KafkaTrigger sample 
        // Consume the message from "topic" on the LocalBroker.
        // Add `BrokerList` and `KafkaPassword` to the local.settings.json
        // For EventHubs
        // "BrokerList": "{EVENT_HUBS_NAMESPACE}.servicebus.windows.net:9093"
        // "KafkaPassword":"{EVENT_HUBS_CONNECTION_STRING}
        [FunctionName("SingleKafkaTriggerQueueOutput")]
        [return: Queue("e2e-dotnet-single-confluent")]
        public static string Run(
            [KafkaTrigger("%BrokerList%",
                          "e2e-kafka-dotnet-single-confluent",
                          Username = "ConfluentCloudUserName",
                          Password = "ConfluentCloudPassword",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string> kevent, ILogger log)
        {
            log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            return kevent.Value;
        }
    }
}
