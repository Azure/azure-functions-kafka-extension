using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


namespace Confluent
{
    public class SingleKafkaTriggerQueueOutput
    {
        // KafkaTrigger sample 
        // Consume the message from "topic" on the LocalBroker.
        // Add `ConfluentBrokerList` and `ConfluentCloudPassword` to the local.settings.json
        // For EventHubs
        // "ConfluentBrokerList": "{EVENT_HUBS_NAMESPACE}.servicebus.windows.net:9093"
        // "ConfluentCloudPassword":"{EVENT_HUBS_CONNECTION_STRING}
        [Function("SingleKafkaTriggerQueueOutput")]
        [QueueOutput("e2e-dotnet-isolated-single-confluent")]
        public static string Run(
            [KafkaTrigger("ConfluentBrokerList",
                          "e2e-kafka-dotnet-isolated-single-confluent",
                          Username = "ConfluentCloudUsername",
                          Password = "ConfluentCloudPassword",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] string eventData, FunctionContext context)
        {
            var logger = context.GetLogger("KafkaFunction");
            logger.LogInformation($"C# Kafka trigger function processed a message: {eventData}");
            var eventDataJson = JObject.Parse(eventData);

            return eventDataJson["Value"].ToString();
        }
    }
}