using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


namespace Eventhub
{
    public class KafkaTriggerManyWithHeaders
    {
        [Function("KafkaTriggerManyWithHeaders")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "topic",
                          Username = "$ConnectionString",
                          Password = "EventHubConnectionString",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default",
                          IsBatched = true)] string[] events, FunctionContext context)
        {
            foreach (var kevent in events)
            {
                var eventJsonObject = JObject.Parse(kevent);
                var logger = context.GetLogger("KafkaFunction");
                logger.LogInformation($"C# Kafka trigger function processed a message: {eventJsonObject["Value"]}");

                var headersJArr = eventJsonObject["Headers"] as JArray;
                logger.LogInformation("Headers for this event: ");
                foreach (JObject header in headersJArr)
                {
                    logger.LogInformation($"{header["Key"]} {System.Text.Encoding.UTF8.GetString((byte[])header["Value"])}");

                }
            }
        }
    }
}