using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Azure.ResourceManager.EventHubs.Models;
using Azure.ResourceManager.Resources;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub
{
    /* Implementation of IQueueManager responsible for management of Azure Eventhub Resource.
    */
    public class EventHubQueueManager : IQueueManager<QueueRequest, QueueResponse>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        private static SemaphoreSlim _semaphore;
        private readonly DefaultAzureCredential credential;
        private static EventHubQueueManager instance = new EventHubQueueManager();
        private ConcurrentDictionary<string, EventHubCollection> queueClientFactory;
        private readonly ILogger logger = TestLogger.TestLogger.logger;
        public static EventHubQueueManager GetInstance()
        {
            return instance;
        }

        private EventHubQueueManager()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            credential = new DefaultAzureCredential();
            queueClientFactory = new ConcurrentDictionary<string, EventHubCollection>();
        }


        public Task clearAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        private async Task<EventHubCollection> GetEventhubCollection(string eventhubNamespace)
        {
            if (queueClientFactory.TryGetValue(eventhubNamespace, out EventHubCollection eventhubCollection)) 
            { 
                return eventhubCollection;
            }

            var client = new ArmClient(credential);
            var subscription = await client.GetDefaultSubscriptionAsync();
            var resourceGroups = subscription.GetResourceGroups();
            var resourceGroup = (await resourceGroups.GetAsync(Constants.RESOURCE_GROUP)).Value;
            
            var namespaceCollection = resourceGroup.GetEventHubNamespaces();
            var eventHubNamespace = (await namespaceCollection.GetAsync(eventhubNamespace)).Value;
            var newEventhubCollection = eventHubNamespace.GetEventHubs();
            
            queueClientFactory.TryAdd(eventhubNamespace, newEventhubCollection);
            
            return newEventhubCollection;
        }

        public async Task createAsync(string queueName)
        {
            int count = 0;


            while (count <= MAX_RETRY_COUNT)
            {
                try
                {
                    await _semaphore.WaitAsync();

                    var eventhubCollection = await GetEventhubCollection(Constants.EVENTHUB_NAMESPACE);
                    EventHubResource eventHub = (await eventhubCollection.CreateOrUpdateAsync(WaitUntil.Completed, queueName, 
                        new EventHubData()
                        {
                            MessageRetentionInDays = 1,
                            PartitionCount = 4                      
                        }
                        )).Value;

                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError($"Exception occured while creating Eventhub {ex}");
                    if (count >= MAX_RETRY_COUNT)
                        throw ex;
                }
                finally
                {
                    _semaphore.Release();
                    count++;
                }
            }
        }

		public async Task deleteAsync(string queueName)
		{
			int count = 0;
			while (count <= MAX_RETRY_COUNT)
			{
				try
				{
                    var eventhubCollection = await GetEventhubCollection(Constants.EVENTHUB_NAMESPACE);
                    var eventhub = (await eventhubCollection.GetAsync(queueName)).Value;
                    await eventhub.DeleteAsync(WaitUntil.Completed);
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
