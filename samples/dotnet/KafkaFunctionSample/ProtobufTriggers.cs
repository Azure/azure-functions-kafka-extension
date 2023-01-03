using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

namespace KafkaFunctionSample
{
    /// <summary>
    /// Demonstrate using protobuf
    /// In this scenario we define the ValueType of the message. The specified type needs to implement IMessage and will be available in the KafkaEventData.Value property.
    /// </summary>
    public static class ProtobufTriggers
    {
        // [FunctionName(nameof(ProtobufUser))]
        // public static void ProtobufUser(
        //     [KafkaTrigger("LocalBroker", "protoUser", ValueType=typeof(ProtoUser), ConsumerGroup = "azfunc")] KafkaEventData[] kafkaEvents,
        //     ILogger logger)
        // {
        //     foreach (var kafkaEvent in kafkaEvents)
        //     {
        //         var user = (ProtoUser)kafkaEvent.Value;
        //         logger.LogInformation($"{JsonConvert.SerializeObject(user)}");
        //     }
        // }
    }
}
