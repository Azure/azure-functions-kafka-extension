using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Eventhub
{
    public class KafkaOutputManyWithHeaders
    {
        [Function("KafkaOutputManyWithHeaders")]

        public static MultipleOutputTypeForBatch Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("HttpFunction");
            log.LogInformation("C# HTTP trigger function processed a request.");
            var response = req.CreateResponse(HttpStatusCode.OK);

            string[] events = new string[2];
            events[0] = "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"one\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"dotnet-isolated\" }] }";
            events[1] = "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"two\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"dotnet-isolated\" }] }";

            return new MultipleOutputTypeForBatch()
            {
                Kevents = events,
                HttpResponse = response
            };
        }
    }
}