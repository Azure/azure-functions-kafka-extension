using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace EventHub
{
    public class MultiKafkaTriggerQueueOutput
    {
        // KafkaTrigger sample 
        // Consume the message from "topic" on the LocalBroker.
        // Add `EventHubBrokerList` and `EventHubConnectionString` to the local.settings.json
        // For EventHubs
        // "EventHubBrokerList": "{EVENT_HUBS_NAMESPACE}.servicebus.windows.net:9093"
        // "EventHubConnectionString":"{EVENT_HUBS_CONNECTION_STRING}
        [FunctionName("MultiKafkaTriggerQueueOutput")]
        public static void Run(
            [KafkaTrigger("EventHubBrokerList",
                          "e2e-kafka-dotnet-multi-eventhub",
                          Username = "$ConnectionString",
                          Password = "%EventHubConnectionString%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string>[] events, ILogger log, 
                          [Queue("e2e-dotnet-multi-eventhub")] ICollector<string> outputArray)
        {
            foreach (KafkaEventData<string> eventData in events)
            {
                log.LogInformation($"C# Kafka trigger function processed a message: {eventData.Value}");
                outputArray.Add(eventData.Value);
            }
        }
    }
}
