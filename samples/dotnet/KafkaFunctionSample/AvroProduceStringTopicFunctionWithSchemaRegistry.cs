using System;
using System.Threading.Tasks;
using Avro;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Avro.Generic;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Trigger;

namespace KafkaFunctionSample
{
    public static class AvroProduceStringTopicFunctionWithSchemaRegistry
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

        /// <summary>
        /// To execute posting an object, execute:
        /// curl -X POST http://localhost:7071/api/AvroProduceStringTopicSchemaRegistry
        ///
        /// To test this, install a schema registry locally with
        /// <see href="https://docs.confluent.io/platform/current/platform-quickstart.html#cp-quick-start-docker">the tutorial from Confluent</see>
        /// and uncomment the function name attribute in this file.
        /// You also have to create a topic called "avroTopic" and add a schema via the Confluent web UI.
        /// The schema that you have to add can be copied from <see cref="PageViewsSchema"/>.
        /// </summary>
        /// <param name="req"></param>
        /// <param name="events"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName(nameof(AvroProduceStringTopicSchemaRegistry))]
        public static async Task<IActionResult> AvroProduceStringTopicSchemaRegistry(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            [SchemaRegistryConfig("schema.registry.url", "localhost:8081")] 
            [Kafka("LocalBroker", "avroTopic")]
            IAsyncCollector<KafkaEventData<GenericRecord>> events,
            ILogger log)
        {
            try
            {
                var genericRecord = new GenericRecord((RecordSchema)Schema.Parse(PageViewsSchema));
                genericRecord.Add("viewtime", 4711L);
                genericRecord.Add("userid", "User4711");
                genericRecord.Add("pageid", "Page4711");
                var kafkaEvent = new KafkaEventData<GenericRecord>()
                {
                    Value = genericRecord,
                };

                await events.AddAsync(kafkaEvent);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Are you sure the topic 'stringTopic' exists? To create using Confluent Docker quickstart run this command: 'docker-compose exec broker kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions'",
                    ex);
            }

            return new OkResult();
        }
    }
}