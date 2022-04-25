using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


namespace Confluent
{
    public class KafkaTriggerManyWithHeaders
    {
        [Function("KafkaTriggerManyWithHeaders")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "topic",
                          Username = "ConfluentCloudUserName",
                          Password = "ConfluentCloudPassword",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default",
                          IsBatched = true)] string[] events, FunctionContext context)
        {
            foreach (var kevent in events)
            {
                var logger = context.GetLogger("KafkaFunction");
                logger.LogInformation($"C# Kafka trigger function processed a message: {JObject.Parse(kevent)["Value"]}");
            }
        }
    }
}