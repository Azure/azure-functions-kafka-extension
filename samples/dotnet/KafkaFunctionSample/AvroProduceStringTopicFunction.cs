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

namespace KafkaFunctionSample
{
    public static class AvroProduceStringTopicFunction
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
        /// curl -X POST http://localhost:7071/api/AvroProduceStringTopic
        /// </summary>
        /// <param name="req"></param>
        /// <param name="events"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName(nameof(AvroProduceStringTopic))]
        public static async Task<IActionResult> AvroProduceStringTopic(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            [Kafka("LocalBroker", "avroTopic", AvroSchema = PageViewsSchema)]
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