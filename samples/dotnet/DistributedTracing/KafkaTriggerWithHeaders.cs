using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace DistributedTracing
{
    public class KafkaTriggerSingleWithHeaders
    {
        [FunctionName("KafkaTriggerSingleWithHeaders")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "topic",
                          Username = "$ConnectionString",
                          Password = "%EventHubConnectionString%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string> kevent, ILogger log)
        {
            log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            log.LogInformation("Headers: ");
            var headers = kevent.Headers;
            foreach (var header in headers)
            {
                log.LogInformation($"Key = {header.Key} Value = {System.Text.Encoding.UTF8.GetString(header.Value)}");
            }
            var activity = Activity.Current;
            Console.WriteLine("Activity Id: " + activity);
            Console.WriteLine("Parent Activity Id:" + activity.ParentId);
        }
    }
}