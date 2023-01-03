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

namespace KafkaFunctionSample
{
    /// <summary>
    /// Demonstrate using generic avro support
    /// In this scenario we define the schema of the message. An instance of GenericRecord will be available in the KafkaEventData.Value property.
    /// </summary>

    public static class AvroGenericTriggers
    {
      const string PageViewsSchema = @"{
  ""type"": ""record"",
  ""name"": ""pageviews"",
  ""namespace"": ""ksql"",
  ""fields"": [
    {
      ""name"": ""viewtime"",
      ""type"": ""long""
    },
    {
      ""name"": ""userid"",
      ""type"": ""string""
    },
    {
      ""name"": ""pageid"",
      ""type"": ""string""
    }
  ]
}";

        [FunctionName(nameof(PageViews))]
        public static void PageViews(
           [KafkaTrigger("LocalBroker", "pageviews", AvroSchema = PageViewsSchema, ConsumerGroup = "azfunc")] KafkaEventData<string, GenericRecord>[] kafkaEvents,
           long[] offsetArray,
           int[] partitionArray,
           string[] topicArray,
           DateTime[] timestampArray,
           ILogger logger)
        {
            for (int i = 0; i < kafkaEvents.Length; i++)
            {
                var kafkaEvent = kafkaEvents[i];
                if (kafkaEvent.Value is GenericRecord genericRecord)
                {
                    logger.LogInformation($"[{timestampArray[i]}] {topicArray[i]} / {partitionArray[i]} / {offsetArray[i]}: {GenericToJson(genericRecord)}");
                }
            }
        }

        public static string GenericToJson(GenericRecord record)
        {
            var props = new Dictionary<string, object>();
            foreach (var field in record.Schema.Fields)
            {
                if (record.TryGetValue(field.Name, out var value))
                    props[field.Name] = value;
            }

            return JsonConvert.SerializeObject(props);
        }
    }
}
