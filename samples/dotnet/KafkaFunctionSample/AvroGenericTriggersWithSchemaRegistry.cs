using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Avro.Generic;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Trigger;


namespace KafkaFunctionSample
{
    /// <summary>
    /// Demonstrate using generic avro support and integrating a Confluent schema registry
    /// In this scenario the schema is retrieved from a registry.
    /// An instance of GenericRecord will be available in the KafkaEventData.Value property.
    ///
    /// This function is disabled by default so it does not interfere with the execution when
    /// no schema registry is present. To test this, install a schema registry locally
    /// with <see href="https://docs.confluent.io/platform/current/platform-quickstart.html#cp-quick-start-docker">the tutorial from Confluent</see>
    /// and uncomment the function name attribute in this file.
    /// </summary>
    /// 
    public class AvroGenericTriggersWithSchemaRegistry
    {
        [FunctionName(nameof(PageViewsSchemaRegistry))]
        public static void PageViewsSchemaRegistry(
            [SchemaRegistryConfig("schema.registry.url", "localhost:8081")]
            [KafkaTrigger("LocalBroker", "pageviews", ConsumerGroup = "azfunc")]
            KafkaEventData<string, GenericRecord>[] kafkaEvents,
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
                    logger.LogInformation(
                        $"[{timestampArray[i]}] {topicArray[i]} / {partitionArray[i]} / {offsetArray[i]}: {GenericToJson(genericRecord)}");
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