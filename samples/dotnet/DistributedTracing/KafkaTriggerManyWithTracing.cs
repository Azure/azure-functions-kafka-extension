using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DistributedTracing
{
    public class KafkaTriggerManyWithTracing
    {
        [FunctionName("KafkaTriggerManyWithTracing")]
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
                var foundTraceParent = eventData.Headers.TryGetFirst("traceparent", out var traceparentInBytes);
                if (foundTraceParent) {
                    var traceparent = Encoding.UTF8.GetString(traceparentInBytes);
                    log.LogInformation($"Traceparent Header for the event: {traceparent}");                    
                }
            }
            var activity = Activity.Current;
            Console.WriteLine("Activity Id: " + activity.TraceId);
            Console.WriteLine("Linked Activities:");
            foreach (var item in activity.Links)
            {
                Console.WriteLine(activity.Id);
            }
        }
    }
}