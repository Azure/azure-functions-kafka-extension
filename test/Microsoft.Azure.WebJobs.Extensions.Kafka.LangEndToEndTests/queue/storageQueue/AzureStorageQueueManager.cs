using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue
{
    public class AzureStorageQueueManager : IQueueManager<List<string>, List<string>>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        private readonly string servicePrinciple;
        private readonly string connectionString;
        private static AzureStorageQueueManager instance = new AzureStorageQueueManager();

        public static AzureStorageQueueManager GetInstance()
        {
            return instance;
        }

        private AzureStorageQueueManager()
        {
            // TODO
            // 1. retrieve service principle from environment variables
            // 2. retrieve the namespace name & connection string from env vars
            // add the required params in constructor
        }

        public void clear(string queueName)
        {
            // TODO clear the Azure Storage Queue
            Console.WriteLine("clearing the queue");
            //throw new NotImplementedException();
        }

        public void create(string queueName)
        {
            throw new NotImplementedException();
        }

        public void delete(string queueName)
        {
            throw new NotImplementedException();
        }

        public List<string> read(int batchSize)
        {
            // TODO
            // 1. add the code to read as per the batch size and return the mesages in List of string
            Console.WriteLine("reading from the queue");
            List<string> list = new List<string>();
            list.Add("message");
            return list;
        }

        public List<string> write(List<string> messageEntity)
        {
            throw new NotImplementedException();
        }
    }
}
