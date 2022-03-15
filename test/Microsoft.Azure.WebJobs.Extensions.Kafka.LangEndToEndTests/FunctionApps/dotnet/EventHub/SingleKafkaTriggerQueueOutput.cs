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
        // Add `EventHubBrokerList` and `EventHubConnectionString` to the local.settings.json
        // For EventHubs
        // "EventHubBrokerList": "{EVENT_HUBS_NAMESPACE}.servicebus.windows.net:9093"
        // "EventHubConnectionString":"{EVENT_HUBS_CONNECTION_STRING}
        [FunctionName("SingleKafkaTriggerQueueOutput")]
        [return: Queue("e2e-dotnet-single-eventhub")]
        public static string Run(
            [KafkaTrigger("EventHubBrokerList",
                          "e2e-kafka-dotnet-single-eventhub",
                          Username = "$ConnectionString",
                          Password = "%EventHubConnectionString%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string> kevent, ILogger log)
        {
            log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            return kevent.Value;
        }
    }
}
