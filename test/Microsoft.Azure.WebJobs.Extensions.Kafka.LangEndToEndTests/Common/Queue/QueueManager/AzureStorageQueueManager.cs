// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Implementation of IQueueManager responsible for management of Azure Queue Resource.
	public class AzureStorageQueueManager : IQueueManager<QueueRequest, QueueResponse>
	{
		private readonly string _connectionString;
		private static readonly AzureStorageQueueManager _instance = new();
		private readonly ConcurrentDictionary<string, QueueClient> _queueClientStore;
		private readonly ILogger _logger = TestLogger.GetTestLogger();

		public static AzureStorageQueueManager GetInstance()
		{
			return _instance;
		}

		private AzureStorageQueueManager()
		{
			_connectionString = Environment.GetEnvironmentVariable(Constants.AZURE_WEBJOBS_STORAGE);
			_queueClientStore = new ConcurrentDictionary<string, QueueClient>();
		}

		public async Task ClearAsync(string queueName)
		{
			bool _clientExists = _queueClientStore.TryGetValue(queueName, out QueueClient queueClient);
			if (!_clientExists) 
			{
				queueClient = new QueueClient(_connectionString, queueName);
				_queueClientStore.TryAdd(queueName, queueClient);
			}
			
			await queueClient.CreateIfNotExistsAsync();
			await queueClient.ClearMessagesAsync();
			_logger.LogInformation($"Clearing the queue: {queueName}");
		}

		public Task CreateAsync(string queueName)
		{
			throw new NotImplementedException();
		}

		public Task DeleteAsync(string queueName)
		{
			throw new NotImplementedException();
		}

		public async Task<QueueResponse> ReadAsync(int batchSize, string queueName)
		{
			QueueClient queueClient = _queueClientStore.GetOrAdd(queueName, (queueName) =>
				{
					var client = new QueueClient(_connectionString, queueName);
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
					_logger.LogInformation($"Dequeued message: '{message.Body}'");
					response.AddString(Utils.Base64Decode(message.Body.ToString()));
				}
			}
			return response;
		}

		public Task<QueueResponse> WriteAsync(QueueRequest messageEntity, string queueName)
		{
			throw new NotImplementedException();
		}
	}
}
