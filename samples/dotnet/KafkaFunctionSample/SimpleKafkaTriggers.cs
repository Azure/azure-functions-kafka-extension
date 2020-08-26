using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Avro;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace KafkaFunctionSample
{
    public class SimpleKafkaTriggers
    {
        [FunctionName(nameof(SampleConsumer))]
        public void SampleConsumer(
    [KafkaTrigger(
            "LocalBroker", 
            "myeventhub", 
            ConsumerGroup = "$Default",
            Username = "$ConnectionString",
            Password = "%EventHubConnectionString%",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain)] KafkaEventData<string> kafkaEvent,
    ILogger logger)
        {
            logger.LogInformation(kafkaEvent.Value.ToString());
        }

        [FunctionName(nameof(SampleProducer))]
        public IActionResult SampleProducer(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        [Kafka(
            "LocalBroker",
            "myeventhub",
            Username = "$ConnectionString",
            Password = "%EventHubConnectionString%",
            Protocol = BrokerProtocol.SaslSsl,
            AuthenticationMode = BrokerAuthenticationMode.Plain)] out KafkaEventData<string>[] kafkaEventData,
        ILogger logger)
        {
            var data = new StreamReader(req.Body).ReadToEnd();
            kafkaEventData = new[] {
                    new KafkaEventData<string>()
                    {
                        Value = data + ":1:" + DateTime.UtcNow.Ticks,
                    },
                    new KafkaEventData<string>()
                    {
                        Value = data + ":2:" + DateTime.UtcNow.Ticks,
                    },
                };
            return new OkResult();
        }
    }
}
