using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace KafkaSamples
{
    public class KafkaOutput
    {
        [Function("KafkaOutput")]
        
        public static MultipleOutputType Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("HttpFunction");
            log.LogInformation("C# HTTP trigger function processed a request.");

            string message = req.FunctionContext
                                .BindingContext
                                .BindingData["message"]
                                .ToString();

            var response = req.CreateResponse(HttpStatusCode.OK);
            return new MultipleOutputType()
            {
                Kevent = message,
                HttpResponse = response
            };
        }
    }

    public class MultipleOutputType
    {
        [KafkaOutput("BrokerList",
                    "kafkaeventhubtest1",
                    Username = "$ConnectionString",
                    Password = "%KafkaPassword%",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain
        )]        
        public string Kevent { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
