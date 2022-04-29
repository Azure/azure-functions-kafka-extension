using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace Confluent
{
    public class KafkaOutput
    {
        [FunctionName("KafkaOutput")]
        public static IActionResult Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Kafka("BrokerList",
                    "topic",
                    Username = "ConfluentCloudUserName",
                    Password = "ConfluentCloudPassword",
                    Protocol = BrokerProtocol.SaslSsl,
                   AuthenticationMode = BrokerAuthenticationMode.Plain
            )] out string eventData,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string message = req.Query["message"];

            string responseMessage = "Ok";            
            eventData = message;

            return new OkObjectResult(responseMessage);
        }
    }
}