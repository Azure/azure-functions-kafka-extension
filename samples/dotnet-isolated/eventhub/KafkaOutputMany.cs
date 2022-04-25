using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Eventhub
{
    public class KafkaOutputMany
    {
        [Function("KafkaOutputMany")]
        
        public static MultipleOutputTypeForBatch Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("HttpFunction");
            log.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);

            string[] messages = new string[2];
            messages[0] = "one";
            messages[1] = "two";

            return new MultipleOutputTypeForBatch()
            {
                Kevents = messages,
                HttpResponse = response
            };
        }
    }

    public class MultipleOutputTypeForBatch
    {
        [KafkaOutput("BrokerList",
                     "topic",
                     Username = "$ConnectionString",
                     Password = "EventHubConnectionString",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain
        )]        
        public string[] Kevents { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
