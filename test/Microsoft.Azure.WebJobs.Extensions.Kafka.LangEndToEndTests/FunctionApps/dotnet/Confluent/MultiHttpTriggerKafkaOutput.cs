using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace Confluent
{
    public class MultiHttpTriggerKafkaOutput
    {
        // KafkaOutputBinding sample
        // This KafkaOutput binding will create a topic "topic" on the LocalBroker if it doesn't exists.
        // Call this function then the KafkaTrigger will be trigged.
        [FunctionName("MultiHttpTriggerKafkaOutput")]
        public static IActionResult Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Kafka("BrokerList",
                    "e2e-kafka-dotnet-multi-confluent",
                    Username = "ConfluentCloudUserName",
                    Password = "ConfluentCloudPassword",
                    Protocol = BrokerProtocol.SaslSsl,
                    AuthenticationMode = BrokerAuthenticationMode.Plain)] out KafkaEventData<string>[] eventData,
                    ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string message = req.Query["message"];
            string message1 = req.Query["message1"];
            string message2 = req.Query["message2"];

            string responseMessage = string.IsNullOrEmpty(message)
                ? "This HTTP triggered function executed successfully. Pass a message in the query string"
                : $"Message {message} {message1} {message2} sent to the broker. This HTTP triggered function executed successfully.";
            eventData = new KafkaEventData<string>[3];
            eventData[0] = new KafkaEventData<string>(message);
            eventData[1] = new KafkaEventData<string>(message1);
            eventData[2] = new KafkaEventData<string>(message2);


            return new OkObjectResult(responseMessage);
        }
    }
}
