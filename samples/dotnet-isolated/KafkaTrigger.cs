using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;


namespace KafkaSamples
{
    public class KafkaTrigger
    {
        [Function("KafkaTrigger")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "kafkaeventhubtest1",
                          Username = "$ConnectionString",
                          Password = "%KafkaPassword%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] string eventData, FunctionContext context)
        {
            var logger = context.GetLogger("KafkaFunction");
            logger.LogInformation($"C# Kafka trigger function processed a message: {JObject.Parse(eventData)["Value"]}");
        }
    }
}