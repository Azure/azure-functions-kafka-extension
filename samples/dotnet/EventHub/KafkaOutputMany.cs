using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace Eventhub
{
    public class KafkaOutputMany
    {
        [FunctionName("KafkaOutputMany")]
        public static IActionResult Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Kafka("BrokerList",
                    "topic",
                    Username = "$ConnectionString",
                    Password = "%EventHubConnectionString%",
                    Protocol = BrokerProtocol.SaslSsl,
                   AuthenticationMode = BrokerAuthenticationMode.Plain
            )] out KafkaEventData<string>[] eventDataArr,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            eventDataArr = new KafkaEventData<string>[2];
            eventDataArr[0] = new KafkaEventData<string>("one");
            eventDataArr[1] = new KafkaEventData<string>("two");
            return new OkObjectResult("Ok");
        }
    }
}