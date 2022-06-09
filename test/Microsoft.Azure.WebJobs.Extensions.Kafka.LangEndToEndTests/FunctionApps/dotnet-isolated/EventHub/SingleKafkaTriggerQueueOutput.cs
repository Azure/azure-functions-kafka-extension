using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


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
        [Function("SingleKafkaTriggerQueueOutput")]
        [QueueOutput("e2e-dotnet-isolated-single-eventhub")]
        public static string Run(
            [KafkaTrigger("EventHubBrokerList",
                          "e2e-kafka-dotnet-isolated-single-eventhub",
                          Username = "$ConnectionString",
                          Password = "%EventHubConnectionString%",
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