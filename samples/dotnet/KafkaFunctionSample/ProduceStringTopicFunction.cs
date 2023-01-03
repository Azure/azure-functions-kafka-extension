using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using System.Text;

namespace KafkaFunctionSample
{
    public static class ProduceStringTopicFunction
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
        [FunctionName(nameof(ProduceStringTopic))]
        public static async Task<IActionResult> ProduceStringTopic(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Kafka("LocalBroker", "stringTopic")] IAsyncCollector<KafkaEventData<string>> events,
            ILogger log)
        {
            try
            {
                var kafkaEvent = new KafkaEventData<string>()
                {
                    Value = await new StreamReader(req.Body).ReadToEndAsync(),
                };

                await events.AddAsync(kafkaEvent);
            }
            catch (Exception ex)
            {
                throw new Exception("Are you sure the topic 'stringTopic' exists? To create using Confluent Docker quickstart run this command: 'docker-compose exec broker kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions'", ex);
            }

            return new OkResult();
        }

        [FunctionName(nameof(ProduceStringTopicOutArrayParameter))]
        public static IActionResult ProduceStringTopicOutArrayParameter(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Kafka("LocalBroker", "stringTopic")] out KafkaEventData<string>[] kafkaEventData,
            ILogger log)
        {
            try
            {
                var data = new StreamReader(req.Body).ReadToEnd();
                kafkaEventData = new[] {
                    new KafkaEventData<string>()
                    {
                        Value = data + ":1:" + DateTime.UtcNow.Ticks,
                    },
                    new KafkaEventData<string>()
                    {
                        Value = data + ":2:" + DateTime.UtcNow.Ticks,
                    },
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Are you sure the topic 'stringTopic' exists? To create using Confluent Docker quickstart run this command: 'docker-compose exec broker kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions'", ex);
            }

            return new OkResult();
        }

        [FunctionName(nameof(ProduceStringTopicOutStringParameter))]
        public static IActionResult ProduceStringTopicOutStringParameter(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Kafka("LocalBroker", "stringTopic")] out string kafkaEventData,
            ILogger log)
        {
            try
            {
                var data = new StreamReader(req.Body).ReadToEnd();
                kafkaEventData = data + ":1:" + DateTime.UtcNow.Ticks;
            }
            catch (Exception ex)
            {
                throw new Exception("Are you sure the topic 'stringTopic' exists? To create using Confluent Docker quickstart run this command: 'docker-compose exec broker kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions'", ex);
            }

            return new OkResult();
        }

        [FunctionName(nameof(ProduceStringTopicOutByteArrayParameter))]
        public static IActionResult ProduceStringTopicOutByteArrayParameter(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Kafka("LocalBroker", "stringTopic")] out byte[] kafkaEventData,
            ILogger log)
        {
            try
            {
                var data = new StreamReader(req.Body).ReadToEnd();
                kafkaEventData = Encoding.UTF8.GetBytes(data + ":1:" + DateTime.UtcNow.Ticks);
            }
            catch (Exception ex)
            {
                throw new Exception("Are you sure the topic 'stringTopic' exists? To create using Confluent Docker quickstart run this command: 'docker-compose exec broker kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions'", ex);
            }

            return new OkResult();
        }
    }
}
