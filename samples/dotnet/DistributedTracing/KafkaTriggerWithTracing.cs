using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;

namespace DistributedTracing
{
    public class KafkaTriggerWithTracing
    {
        [FunctionName("KafkaTriggerWithTracing")]
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
            var foundTraceParent = kevent.Headers.TryGetFirst("traceparent", out var traceparentInBytes);
            if (foundTraceParent) {
                var traceparent = Encoding.UTF8.GetString(traceparentInBytes);
                log.LogInformation($"Traceparent Header for the event: {traceparent}");                    
            }
            var activity = Activity.Current;
            Console.WriteLine("Activity Id: " + activity.TraceId);
        }
    }
}