using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub
{
    public class EventHubQueueManager : IQueueManager<QueueRequest, QueueResponse>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        private static SemaphoreSlim _semaphore;
        private readonly string servicePrinciple;
        private readonly string connectionString;
        //Does this even work?
        private static EventHubQueueManager instance = new EventHubQueueManager();
        private EventHubsManagementClient eventHubsManagementClient;
        public static EventHubQueueManager GetInstance()
        {
            return instance;
        }

        private EventHubQueueManager()
        {
            // TODO
            // 1. retrieve service principle from environment variables
            // 2. retrieve the namespace name & connection string from env vars
            // add the required params in constructor
            _semaphore = new SemaphoreSlim(1, 1);
            string subscriptionId = Environment.GetEnvironmentVariable(Constants.AZURE_SUBSCRIPTION_ID);
            var credential = new DefaultAzureCredential();
            eventHubsManagementClient = new EventHubsManagementClient(subscriptionId, credential);

            //Create a dictionary -- Not required since Namespace scope
        }

        public Task clearAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task createAsync(string queueName)
        {
            int count = 0;


            while (count <= MAX_RETRY_COUNT)
            {
                try
                {
                    // TODO
                    // 1. check if already exists
                    //  1.1 clear the eventhub or delete that

                    // DONE
                    // 2. create the new eventhub
                    // 2.1 if creation failed retry three times
                    // return if success

                    //var eventhublist = eventHubManagementClient.EventHubs.ListByNamespaceAsync(Constants.RESOURCE_GROUP, Constants.EVENTHUB_NAMESPACE);
                    await _semaphore.WaitAsync();
                    var newEventHubresponse = await eventHubsManagementClient.EventHubs.CreateOrUpdateAsync(Constants.RESOURCE_GROUP, Constants.EVENTHUB_NAMESPACE, queueName,
                        new Eventhub()
                        {
                            MessageRetentionInDays = 1,
                            PartitionCount = 4
                        });
                    Console.WriteLine(newEventHubresponse.ToString());
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    if (count >= MAX_RETRY_COUNT)
                        throw ex;
                }
                finally
                {
                    _semaphore.Release();
                    count++;
                }
            }
            // what to do here?
        }

        public async Task deleteAsync(string queueName)
        {
            int count = 0;
            while (count <= MAX_RETRY_COUNT)
            {
                try
                {
                    await eventHubsManagementClient.EventHubs.DeleteAsync(Constants.RESOURCE_GROUP, Constants.EVENTHUB_NAMESPACE, queueName);
                    return;
                }
                catch (Exception ex)
                {
                    if (count >= MAX_RETRY_COUNT)
                        throw ex;
                }
                finally
                {
                    count++;
                }
            }
        }

        public Task<QueueResponse> readAsync(int batchSize, string queueName)
        {
            throw new NotImplementedException();
        }

        public Task<QueueResponse> writeAsync(QueueRequest writeRequest, string queueName)
        {
            throw new NotImplementedException();
        }
    }
}
