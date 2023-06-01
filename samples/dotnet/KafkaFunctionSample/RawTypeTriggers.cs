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
        //    [KafkaTrigger("LocalBroker", "stringTopic", ConsumerGroup = "azfunc")] KafkaEventData<string> kafkaEvent,
        //    ILogger logger)
        //{
        //    logger.LogInformation(kafkaEvent.Value.ToString());
        //}

        //[FunctionName(nameof(ProtoUserBinary))]
        //public static void ProtoUserBinary(
        //   [KafkaTrigger("LocalBroker", "protoUser", ConsumerGroup = "azfunc")] KafkaEventData<Confluent.Kafka.Ignore, ProtoUser> kafkaEvent,
        //   ILogger logger)
        //{
        //    logger.LogInformation(kafkaEvent.Value.ToString());
        //}
    }
}
