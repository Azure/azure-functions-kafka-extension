using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

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
        //    [KafkaTrigger("LocalBroker", "stringTopic", ConsumerGroup = "azfunc", ValueType = typeof(string))] KafkaEventData kafkaEvent,
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
