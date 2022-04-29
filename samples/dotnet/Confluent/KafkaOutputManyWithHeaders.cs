using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace Confluent
{
    public class KafkaOutputManyWithHeaders
    {
        [FunctionName("KafkaOutputManyWithHeaders")]
        public static IActionResult Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Kafka("BrokerList",
                    "topic",
                    Username = "ConfluentCloudUserName",
                    Password = "ConfluentCloudPassword",
                    Protocol = BrokerProtocol.SaslSsl,
                   AuthenticationMode = BrokerAuthenticationMode.Plain
            )] out KafkaEventData<string>[] eventDataArr,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            eventDataArr = new KafkaEventData<string>[2];
            eventDataArr[0] = new KafkaEventData<string>("one");
            eventDataArr[0].Headers.Add("test", System.Text.Encoding.UTF8.GetBytes("dotnet"));
            eventDataArr[1] = new KafkaEventData<string>("two");
            eventDataArr[1].Headers.Add("test1", System.Text.Encoding.UTF8.GetBytes("dotnet"));
            return new OkObjectResult("Ok");
        }
    }
}