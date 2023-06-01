using System.Threading.Tasks;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace KafkaFunctionSample
{
    /// <summary>
    /// Demonstrate using specific avro support
    /// In this scenario we define the ValueType of the message. The specified type needs to implement ISpecificRecord and will be available in the KafkaEventData.Value property.
    /// </summary>
    public static class AvroSpecificTriggers
    {
        //[FunctionName(nameof(User))]
        //public static void User(
        //   [KafkaTrigger("LocalBroker", "users", ConsumerGroup = "azfunc")] KafkaEventData<string, UserRecord>[] kafkaEvents,
        //   ILogger logger)
        //{
        //    foreach (var kafkaEvent in kafkaEvents)
        //    {
        //        logger.LogInformation($"{JsonConvert.SerializeObject(kafkaEvent.Value)}");
        //    }
        //}

        // private static AvroDeserializer<UserRecord> myCustomDeserialiser = new AvroDeserializer<UserRecord>(new LocalSchemaRegistry(UserRecord.SchemaText));

        ///// <summary>
        ///// This function shows how to implement a custom deserialiser in the function method
        ///// </summary>
        ///// <param name="kafkaEvents">Kafka events.</param>
        ///// <param name="logger">Logger.</param>
        //[FunctionName(nameof(UserAsBytes))]
        //public static async Task UserAsBytes(
        //   [KafkaTrigger("LocalBroker", "users", ConsumerGroup = "azfunc_bytes")] byte[][] kafkaEvents,
        //   ILogger logger)
        //{
        //    foreach (var kafkaEvent in kafkaEvents)
        //    {
        //        var desUserRecord = await myCustomDeserialiser.DeserializeAsync(kafkaEvent, false, Confluent.Kafka.SerializationContext.Empty);
        //        logger.LogInformation($"Custom deserialised user from batch: {JsonConvert.SerializeObject(desUserRecord)}");
        //    }
        //}

        ///// <summary>
        ///// This function shows how to implement a custom deserialiser in the function method
        ///// </summary>
        ///// <param name="kafkaEvent">Kafka events.</param>
        ///// <param name="logger">Logger.</param>
        //[FunctionName(nameof(UserAsByte))]
        //public static async Task UserAsByte(
        //   [KafkaTrigger("LocalBroker", "users", ConsumerGroup = "azfunc_byte")] KafkaEventData<Confluent.Kafka.Ignore, byte[]> kafkaEvent,
        //   ILogger logger)
        //{
        //    var desUserRecord = await myCustomDeserialiser.DeserializeAsync(kafkaEvent.Value, false, Confluent.Kafka.SerializationContext.Empty);
        //    logger.LogInformation($"Custom deserialised user: {JsonConvert.SerializeObject(desUserRecord)}");
        //}

        //[FunctionName(nameof(PageViewsFemale))]
        //public static void PageViewsFemale(
        //   [KafkaTrigger("LocalBroker", "PAGEVIEWS_FEMALE", ConsumerGroup = "azfunc")] KafkaEventData<Confluent.Kafka.Null, PageViewsFemale>[] kafkaEvents,
        //   ILogger logger)
        //{
        //    foreach (var ke in kafkaEvents)
        //    {
        //        logger.LogInformation($"{JsonConvert.SerializeObject(ke.Value)}");
        //    }
        //}
    }
}
