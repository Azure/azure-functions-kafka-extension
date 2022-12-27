using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Tracing
{
    public class KafkaTriggerManyWithHeaders
    {
        [FunctionName("KafkaTriggerManyWithHeaders")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "topic",
                          Username = "$ConnectionString",
                          Password = "%EventHubConnectionString%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string>[] events, ILogger log)
        {
            foreach (KafkaEventData<string> eventData in events)
            {
                log.LogInformation($"C# Kafka trigger function processed a message: {eventData.Value}");
                log.LogInformation($"Headers: ");
                var headers = eventData.Headers;
                foreach (var header in headers)
                {
                    log.LogInformation($"Key = {header.Key} Value = {System.Text.Encoding.UTF8.GetString(header.Value)}");
                }
            }
            var activity = Activity.Current;
            Console.WriteLine("Activity Id: " + activity);
            Console.WriteLine("Linked Activities:");
            foreach (var item in activity.Links)
            {
                Console.WriteLine(activity.Id);
            }
        }
    }
}