using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Confluent
{
    public class KafkaOutputWithHeaders
    {
        [Function("KafkaOutputWithHeaders")]

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
            string kevent = "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"" + message + "\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"dotnet-isolated\" }] }";
            var response = req.CreateResponse(HttpStatusCode.OK);
            return new MultipleOutputType()
            {
                Kevent = kevent,
                HttpResponse = response
            };
        }
    }
}