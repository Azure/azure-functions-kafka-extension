using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;

namespace KafkaSamples
{
    public class KafkaOutputMany
    {
        [FunctionName("KafkaOutputMany")]
        public static IActionResult Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Kafka("BrokerList",
                    "kafkaeventhubtest1",
                    Username = "$ConnectionString",
                    Password = "%KafkaPassword%",
                    Protocol = BrokerProtocol.SaslSsl,
                   AuthenticationMode = BrokerAuthenticationMode.Plain
            )] out KafkaEventData<string>[] eventData,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            eventDataArr = new KafkaEventData<string>[2];
            eventDataArr[0] = new KafkaEventData<string>("one");
            eventDataArr[0].Headers.Add("test", "dotnet");
            eventDataArr[1] = new KafkaEventData<string>("two");
            eventDataArr[1].Headers.Add("test", "dotnet");
            return new OkObjectResult("Ok");
        }
    }
}