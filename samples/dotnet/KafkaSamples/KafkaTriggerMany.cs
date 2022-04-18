using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Storage;
using Microsoft.Extensions.Logging;

namespace KafkaSamples
{
    public class KafkaTrigger 
    {
        [FunctionName("KafkaTrigger")]
        public static void Run(
            [KafkaTrigger("BrokerList",
                          "kafkaeventhubtest1",
                          Username = "$ConnectionString",
                          Password = "%KafkaPassword%",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "$Default")] KafkaEventData<string>[] events, ILogger log)
        {       
            foreach (KafkaEventData<string> kevent in events)
            {    
                log.LogInformation($"C# Kafka trigger function processed a message: {kevent.Value}");
            }
        }
    }
}