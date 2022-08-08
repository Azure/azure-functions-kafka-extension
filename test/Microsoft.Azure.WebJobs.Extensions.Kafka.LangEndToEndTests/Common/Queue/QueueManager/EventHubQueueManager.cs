// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.EventHubs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Implementation of IQueueManager responsible for management of Azure Eventhub Resource.
	public class EventHubQueueManager : IQueueManager<QueueRequest, QueueResponse>
	{
		private readonly static int _MAX_RETRY_COUNT = 3;
		private readonly SemaphoreSlim _semaphore;
		private readonly DefaultAzureCredential _credential;
		private static readonly EventHubQueueManager _instance = new();
		private readonly ConcurrentDictionary<string, EventHubCollection> _queueClientStore;
		private readonly ILogger _logger = TestLogger.GetTestLogger();

		public static EventHubQueueManager GetInstance()
		{
			return _instance;
		}

		private EventHubQueueManager()
		{
			_semaphore = new SemaphoreSlim(1, 1);
			_credential = new DefaultAzureCredential();
			_queueClientStore = new ConcurrentDictionary<string, EventHubCollection>();
		}


		public Task ClearAsync(string queueName)
		{
			throw new NotImplementedException();
		}

		private async Task<EventHubCollection> GetEventhubCollection(string eventhubNamespace)
		{
			if (_queueClientStore.TryGetValue(eventhubNamespace, out EventHubCollection eventhubCollection))
			{
				return eventhubCollection;
			}

			var client = new ArmClient(_credential);
			var subscription = await client.GetDefaultSubscriptionAsync();
			var resourceGroups = subscription.GetResourceGroups();
			var resourceGroup = (await resourceGroups.GetAsync(Constants.RESOURCE_GROUP)).Value;

			var namespaceCollection = resourceGroup.GetEventHubsNamespaces();
			var eventHubNamespace = (await namespaceCollection.GetAsync(eventhubNamespace)).Value;
			var newEventhubCollection = eventHubNamespace.GetEventHubs();

			_queueClientStore.TryAdd(eventhubNamespace, newEventhubCollection);

			return newEventhubCollection;
		}

		public async Task CreateAsync(string queueName)
		{
			int count = 0;


			while (count <= _MAX_RETRY_COUNT)
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
					_logger.LogError($"Exception occured while creating Eventhub {ex}");
					if (count >= _MAX_RETRY_COUNT)
						throw ex;
				}
				finally
				{
					_semaphore.Release();
					count++;
				}
			}
		}

		public async Task DeleteAsync(string queueName)
		{
			int count = 0;
			while (count <= _MAX_RETRY_COUNT)
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
					if (count >= _MAX_RETRY_COUNT)
						throw ex;
				}
				finally
				{
					count++;
				}
			}
		}
		public Task<QueueResponse> ReadAsync(int batchSize, string queueName)
		{
			throw new NotImplementedException();
		}

		public Task<QueueResponse> WriteAsync(QueueRequest writeRequest, string queueName)
		{
			throw new NotImplementedException();
		}
	}
}
