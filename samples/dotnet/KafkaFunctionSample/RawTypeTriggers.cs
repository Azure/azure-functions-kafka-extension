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
using Avro.Generic;
using System.Collections.Generic;
using System.Linq;

namespace KafkaFunctionSample
{
    /// <summary>
    /// Demonstrate using raw type triggers
    /// To enable this demo you must create new topics (stringTopic, protoUser) and produce data
    /// </summary>

    public static class RawTypeTriggers
    {
        //[FunctionName(nameof(StringTopic))]
        //public static void StringTopic(
        //    [KafkaTrigger("LocalBroker", "stringTopic", ConsumerGroup = "azfunc")] KafkaEventData kafkaEvent,
        //    ILogger logger)
        //{
        //    logger.LogInformation(kafkaEvent.Value.ToString());
        //}

        //[FunctionName(nameof(ProtoUserBinary))]
        //public static void ProtoUserBinary(
        //   [KafkaTrigger("LocalBroker", "protoUser", ValueType=typeof(byte[]), ConsumerGroup = "azfunc")] KafkaEventData kafkaEvent,
        //   ILogger logger)
        //{
        //   logger.LogInformation(kafkaEvent.Value.ToString());
        //}
    }
}
