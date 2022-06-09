using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


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
        [Function("MultiKafkaTriggerQueueOutput")]
        [QueueOutput("e2e-dotnet-isolated-multi-confluent")]
        public static string[] Run(
            [KafkaTrigger("ConfluentBrokerList",
                          "e2e-kafka-dotnet-isolated-multi-confluent",
                          Username = "ConfluentCloudUsername",
                          Password = "ConfluentCloudPassword",
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