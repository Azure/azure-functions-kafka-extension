using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue
{
    public class AzureStorageQueueManager : IQueueManager<QueueRequest, QueueResponse>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        private readonly string servicePrinciple;
        private readonly string connectionString;
        private static AzureStorageQueueManager instance = new AzureStorageQueueManager();
        private ConcurrentDictionary<string, QueueClient> queueClientFactory;
        public static AzureStorageQueueManager GetInstance()
        {
            return instance;
        }

        private AzureStorageQueueManager()
        {
            connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);
            //Populate the dictionary with 12 clients: QueueName Value: AzureStorageClient(conn string, queueName)
            //Use the ConcurrentDictionary Class
            // Key: QueueName Value: QueueClient
            queueClientFactory = new ConcurrentDictionary<string, QueueClient>();
        }

        public async Task clearAsync(string queueName)
        {
            // TODO clear the Azure Storage Queue
            QueueClient queueClient = new QueueClient(connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.ClearMessagesAsync();
            queueClientFactory.GetOrAdd(queueName, queueClient);
            Console.WriteLine("clearing the queue");
            //throw new NotImplementedException();
        }

        public Task createAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public Task deleteAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task<QueueResponse> readAsync(int batchSize, string queueName)
        {
            // TODO
            // 1. add the code to read as per the batch size and return the mesages in List of string
            QueueClient queueClient = queueClientFactory.GetOrAdd(queueName, (queueName) =>
                { 
                    var client = new QueueClient(connectionString, queueName);
                    client.CreateIfNotExists();
                    return client;
                }
            );

            QueueResponse response = new QueueResponse();

            if (queueClient.Exists())
            {
                QueueMessage[] retrievedMessage = await queueClient.ReceiveMessagesAsync(batchSize);
                foreach (QueueMessage message in retrievedMessage)
                {
                    Console.WriteLine($"Dequeued message: '{message.Body}'");
                    response.AddString(Utils.Base64Decode(message.Body.ToString()));
                }
            }
            return response;
        }

        public Task<QueueResponse> writeAsync(QueueRequest messageEntity, string queueName)
        {
            throw new NotImplementedException();
        }
    }
}
