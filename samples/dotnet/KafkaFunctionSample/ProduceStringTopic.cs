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
    public static class ProduceStringTopic
    {
        /// <summary>
        /// Make sure the topic "stringTopicTenPartitions" exists
        /// To send data using curl:
        /// curl -d "hello world" -X POST http://localhost:7071/api/ProduceStringTopic
        /// </summary>
        /// <param name="req"></param>
        /// <param name="events"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("ProduceStringTopic")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Kafka("stringTopicTenPartitions", BrokerList = "LocalBroker")] IAsyncCollector<KafkaEventData> events,
            ILogger log)
        {
            try
            {
                var kafkaEvent = new KafkaEventData()
                {
                    Value = await new StreamReader(req.Body).ReadToEndAsync(),
                };

                await events.AddAsync(kafkaEvent);
            }
            catch (Exception ex)
            {
                throw new Exception("Are you sure the topic 'stringTopicTenPartitions' exists? To created using Confluent Docker quickstart run this command: 'docker-compose exec broker kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions'", ex);
            }

            return new OkResult();
        }
    }
}
