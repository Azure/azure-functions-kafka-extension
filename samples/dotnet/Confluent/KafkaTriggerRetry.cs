using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;
using System;

namespace Confluent
{
    public class KafkaTriggerRetry 
    {
        [FunctionName("KafkaTriggerRetry")]
        [FixedDelayRetry(3, "00:00:03")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "topic",
                          Username = "ConfluentCloudUserName",
                          Password = "ConfluentCloudPassword",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string> kevent, ILogger log)
        {            
            log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            throw new Exception("Unhandled Error");
        }

        [FunctionName("KafkaTriggerExponentialRetry")]
        [ExponentialBackoffRetry(-1, "00:00:05", "00:01:00")]
        public static void RunExponential(
            [KafkaTrigger("BrokerList",
                          "topic",
                          Username = "ConfluentCloudUserName",
                          Password = "ConfluentCloudPassword",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string> kevent, ILogger log)
        {            
            log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            throw new Exception("Unhandled KafkaTriggerExponentialRetry Error");
        }
    }
}