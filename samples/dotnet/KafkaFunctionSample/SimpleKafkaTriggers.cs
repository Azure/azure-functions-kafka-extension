using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Avro;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Text;

namespace KafkaFunctionSample
{
    public class SimpleKafkaTriggers
    {
        [FunctionName(nameof(ConsoleConsumer))]
        public void ConsoleConsumer(
        [KafkaTrigger(
            "LocalBroker",
            "stringTopicTenPartitions",
            ConsumerGroup = "$Default",
            AuthenticationMode = BrokerAuthenticationMode.Plain)] KafkaEventData<string>[] kafkaEvents,
            ILogger logger)
        {
            foreach(var kafkaEvent in kafkaEvents)
                logger.LogInformation(kafkaEvent.Value.ToString());
        }

        [FunctionName(nameof(ConsoleProducer))]
        public static IActionResult ConsoleProducer(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        [Kafka("LocalBroker", "stringTopicTenPartitions")] out string kafkaEventData,
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

        // EventHubs Configuration sample
        //
        //[FunctionName(nameof(SampleConsumer))]
        //public void SampleConsumer(
        //[KafkaTrigger(
        //    "LocalBroker",
        //    "%EHTOPIC%",
        //    ConsumerGroup = "$Default",
        //    Username = "$ConnectionString",
        //    Password = "%EventHubConnectionString%",
        //    Protocol = BrokerProtocol.SaslSsl,
        //    AuthenticationMode = BrokerAuthenticationMode.Plain)] KafkaEventData<string> kafkaEvent,
        //ILogger logger)
        //{
        //    logger.LogInformation(kafkaEvent.Value.ToString());
        //}

        // EventHubs Configuration sample
        //
        //[FunctionName(nameof(SampleProducer))]
        //public IActionResult SampleProducer(
        //[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        //[Kafka(
        //    "LocalBroker",
        //    "%EHTOPIC%",
        //    Username = "$ConnectionString",
        //    Password = "%EventHubConnectionString%",
        //    Protocol = BrokerProtocol.SaslSsl,
        //    AuthenticationMode = BrokerAuthenticationMode.Plain)] out KafkaEventData<string>[] kafkaEventData,
        //ILogger logger)
        //{
        //    var data = new StreamReader(req.Body).ReadToEnd();
        //    kafkaEventData = new[] {
        //            new KafkaEventData<string>()
        //            {
        //                Value = data + ":1:" + DateTime.UtcNow.Ticks,
        //            },
        //            new KafkaEventData<string>()
        //            {
        //                Value = data + ":2:" + DateTime.UtcNow.Ticks,
        //            },
        //        };
        //    return new OkResult();
        //}
    }
}
