using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Confluent
{
    public class MultiKafkaTriggerQueueOutput
    {
        // KafkaTrigger sample 
        // Consume the message from "topic" on the LocalBroker.
        // Add `ConfluentBrokerList` and `ConfluentCloudPassword` to the local.settings.json
        // For EventHubs
        // "ConfluentBrokerList": "{EVENT_HUBS_NAMESPACE}.servicebus.windows.net:9093"
        // "ConfluentCloudPassword":"{EVENT_HUBS_CONNECTION_STRING}
        [FunctionName("MultiKafkaTriggerQueueOutput")]
        public static void Run(
            [KafkaTrigger("ConfluentBrokerList",
                          "e2e-kafka-dotnet-multi-confluent",
                          Username = "ConfluentCloudUsername",
                          Password = "ConfluentCloudPassword",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string>[] events, ILogger log, 
                          [Queue("e2e-dotnet-multi-confluent")] ICollector<string> outputArray)
        {
            foreach (KafkaEventData<string> eventData in events)
            {
                log.LogInformation($"C# Kafka trigger function processed a message: {eventData.Value}");
                outputArray.Add(eventData.Value);
            }
        }
    }
}
