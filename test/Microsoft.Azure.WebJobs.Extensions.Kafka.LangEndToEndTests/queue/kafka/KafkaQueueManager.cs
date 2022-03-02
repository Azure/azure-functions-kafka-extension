using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.kafka
{
    // TODO for future refernce
    public class KafkaQueueManager : IQueueManager<string, string>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        //private readonly string username;
        //private readonly string apiKey;
        private static KafkaQueueManager instance = new KafkaQueueManager();

        public KafkaQueueManager GetInstance()
        {
            return instance;
        }

        public void clear(string queueName)
        {
            throw new NotImplementedException();
        }

        public void create(string queueName)
        {
            throw new NotImplementedException();
        }

        public void delete(string queueName)
        {
            throw new NotImplementedException();
        }

        public string read(int batchSize)
        {
            throw new NotImplementedException();
        }

        public string write(string messageEntity)
        {
            throw new NotImplementedException();
        }
    }
}
