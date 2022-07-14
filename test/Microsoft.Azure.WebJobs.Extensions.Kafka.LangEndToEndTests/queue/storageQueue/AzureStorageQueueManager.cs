using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue
{
    /* Implementation of IQueueManager responsible for management of Azure Queue Resource.
    */
    public class AzureStorageQueueManager : IQueueManager<QueueRequest, QueueResponse>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        private readonly string servicePrinciple;
        private readonly string connectionString;
        private static AzureStorageQueueManager instance = new AzureStorageQueueManager();
        private ConcurrentDictionary<string, QueueClient> queueClientFactory;
        private readonly ILogger logger = TestLogger.TestLogger.GetTestLogger();

        public static AzureStorageQueueManager GetInstance()
        {
            return instance;
        }

        private AzureStorageQueueManager()
        {
            connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);
            queueClientFactory = new ConcurrentDictionary<string, QueueClient>();
        }

        public async Task clearAsync(string queueName)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.ClearMessagesAsync();
            queueClientFactory.GetOrAdd(queueName, queueClient);
            logger.LogInformation($"Clearing the queue: {queueName}");
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
                    logger.LogInformation($"Dequeued message: '{message.Body}'");
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
