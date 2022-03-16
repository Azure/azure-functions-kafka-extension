using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace Confluent
{
    public class SingleHttpTriggerKafkaOutput
    {
        // KafkaOutputBinding sample
        // This KafkaOutput binding will create a topic "topic" on the LocalBroker if it doesn't exists.
        // Call this function then the KafkaTrigger will be trigged.
        [FunctionName("SingleHttpTriggerKafkaOutput")]
        public static IActionResult Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Kafka("ConfluentBrokerList",
                    "e2e-kafka-dotnet-single-confluent",
                    Username = "ConfluentCloudUsername",
                    Password = "ConfluentCloudPassword",
                    Protocol = BrokerProtocol.SaslSsl,
                   AuthenticationMode = BrokerAuthenticationMode.Plain
            )] out string eventData,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string message = req.Query["message"];

            string responseMessage = string.IsNullOrEmpty(message)
                ? "This HTTP triggered function executed successfully. Pass a message in the query string"
                : $"Message {message} sent to the broker. This HTTP triggered function executed successfully.";
            eventData = message;

            return new OkObjectResult(responseMessage);
        }
    }
}