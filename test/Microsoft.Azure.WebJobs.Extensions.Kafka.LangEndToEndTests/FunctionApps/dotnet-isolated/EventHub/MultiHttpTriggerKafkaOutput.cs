using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace EventHub
{
    public class MultiHttpTriggerKafkaOutput
    {
        // KafkaOutputBinding sample
        // This KafkaOutput binding will create a topic "topic" on the LocalBroker if it doesn't exists.
        // Call this function then the KafkaTrigger will be trigged.
        [Function("MultiHttpTriggerKafkaOutput")]
        
        public static MultipleOutputTypeForBatch Output(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequestData req,
            FunctionContext executionContext)
        {
            var log = executionContext.GetLogger("HttpFunction");
            log.LogInformation("C# HTTP trigger function processed a request.");

            string message = req.FunctionContext
                                .BindingContext
                                .BindingData["message"]
                                .ToString();

            string message1 = req.FunctionContext
                                .BindingContext
                                .BindingData["message1"]
                                .ToString();

            string message2 = req.FunctionContext
                                .BindingContext
                                .BindingData["message2"]
                                .ToString();


            string responseMessage = string.IsNullOrEmpty(message)
                ? "This HTTP triggered function executed successfully. Pass a message in the query string"
                : $"Message {message} {message1} {message2} sent to the broker. This HTTP triggered function executed successfully.";
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString(responseMessage);

            string[] messages = new string[3];
            messages[0] = message;
            messages[1] = message1;
            messages[2] = message2;

            return new MultipleOutputTypeForBatch()
            {
                Kevents = messages,
                HttpResponse = response
            };
        }
    }

    public class MultipleOutputTypeForBatch
    {
        [KafkaOutput("EventHubBrokerList",
            "e2e-kafka-dotnet-isolated-multi-eventhub",
            Username = "$ConnectionString",
            Password = "%EventHubConnectionString%",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain
        )]        
        public string[] Kevents { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
}
