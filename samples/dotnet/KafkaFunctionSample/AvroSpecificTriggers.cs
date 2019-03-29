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
    /// Demonstrate using specific avro support
    /// In this scenario we define the ValueType of the message. The specified type needs to implement ISpecificRecord and will be available in the KafkaEventData.Value property.
    /// </summary>
    public static class AvroSpecificTriggers
    {
        //[FunctionName(nameof(User))]
        //public static void User(
        //   [KafkaTrigger("LocalBroker", "users", ValueType=typeof(UserRecord), ConsumerGroup = "azfunc")] KafkaEventData[] kafkaEvents,
        //   ILogger logger)
        //{
        //    foreach (var kafkaEvent in kafkaEvents)
        //    {
        //        logger.LogInformation($"{JsonConvert.SerializeObject(kafkaEvent.Value)}");
        //    }
        //}

        //[FunctionName(nameof(PageViewsFemale))]
        //public static void PageViewsFemale(
        //   [KafkaTrigger("LocalBroker", "PAGEVIEWS_FEMALE", ValueType=typeof(PageViewsFemale), ConsumerGroup = "azfunc")] KafkaEventData[] kafkaEvents,
        //   ILogger logger)
        //{
        //   foreach (var ke in kafkaEvents)
        //   {
        //       logger.LogInformation($"{JsonConvert.SerializeObject(ke.Value)}");
        //   }
        //}
    }
}
