using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue
{
    public class AzureStorageQueueManager : IQueueManager<QueueRequest, QueueResponse>
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

            //Populate the dictionary with 2*2*language queues Key: QueueName Value: QueueClient
        }

        public async Task clearAsync(string queueName)
        {
            // TODO clear the Azure Storage Queue
            Console.WriteLine("clearing the queue");
            //throw new NotImplementedException();
        }

        public async Task createAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task deleteAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task<QueueResponse> readAsync(int batchSize)
        {
            // TODO
            // 1. add the code to read as per the batch size and return the mesages in List of string
            throw new NotImplementedException();
        }

        public async Task<QueueResponse> writeAsync(QueueRequest messageEntity)
        {
            throw new NotImplementedException();
        }
    }
}
