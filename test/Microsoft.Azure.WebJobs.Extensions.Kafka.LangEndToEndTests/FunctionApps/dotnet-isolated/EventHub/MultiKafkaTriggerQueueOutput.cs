using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


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
        [Function("MultiKafkaTriggerQueueOutput")]
        [QueueOutput("e2e-dotnet-isolated-multi-eventhub")]
        public static string[] Run(
            [KafkaTrigger("EventHubBrokerList",
                          "e2e-kafka-dotnet-isolated-multi-eventhub",
                          Username = "$ConnectionString",
                          Password = "%EventHubConnectionString%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default",
                          IsBatched = true)] string[] events, FunctionContext context)
        {
            string[] messages = new string[events.Length];
            for (int i = 0; i < events.Length; i++)
            {
                var logger = context.GetLogger("KafkaFunction");
                logger.LogInformation($"C# Kafka trigger function processed a message: {events[i]}");
                var eventDataJson = JObject.Parse(events[i]);
                var message = eventDataJson["Value"].ToString();
                messages[i] = message;
            }
            return messages;
        }
    }
}