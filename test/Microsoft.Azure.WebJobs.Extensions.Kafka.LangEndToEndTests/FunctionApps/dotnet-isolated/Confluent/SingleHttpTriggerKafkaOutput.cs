using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Confluent
{
    public class SingleHttpTriggerKafkaOutput
    {
        // KafkaOutputBinding sample
        // This KafkaOutput binding will create a topic "topic" on the LocalBroker if it doesn't exists.
        // Call this function then the KafkaTrigger will be trigged.
        [Function("SingleHttpTriggerKafkaOutput")]
        
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

            string responseMessage = string.IsNullOrEmpty(message)
                ? "This HTTP triggered function executed successfully. Pass a message in the query string"
                : $"Message {message} sent to the broker. This HTTP triggered function executed successfully.";
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(responseMessage);
            return new MultipleOutputType()
            {
                Kevent = message,
                HttpResponse = response
            };
        }
    }

    public class MultipleOutputType
    {
        [KafkaOutput("ConfluentBrokerList",
            "e2e-kafka-dotnet-isolated-single-confluent",
            Username = "ConfluentCloudUsername",
            Password = "ConfluentCloudPassword",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain
        )]        
        public string Kevent { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
