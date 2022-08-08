// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Responsible for creation and execution of commands to interact with Queue Type resources(External Resources).
	public class QueueCommand : IExecutableCommand<QueueResponse>
	{
		private readonly QueueType _queueType;
		private readonly QueueOperation _queueOperation;
		private readonly string _queueName;
		private readonly IQueueManager<QueueRequest, QueueResponse> _queueManager;

		public QueueCommand(QueueType queueType, QueueOperation queueOperation, string queueName)
		{
			_queueType = queueType;
			_queueName = queueName;
			_queueOperation = queueOperation;
			if (QueueType.EventHub == queueType)
			{
				_queueManager = EventHubQueueManager.GetInstance();
			}
			else if (QueueType.AzureStorageQueue == queueType)
			{
				_queueManager = AzureStorageQueueManager.GetInstance();
			}
		}

		public async Task<QueueResponse> ExecuteCommandAsync()
		{
			QueueResponse response = null;

			switch (_queueOperation)
			{
				case QueueOperation.CREATE:
					await _queueManager.CreateAsync(_queueName);
					break;
				case QueueOperation.DELETE:
					await _queueManager.DeleteAsync(_queueName);
					break;
				case QueueOperation.CLEAR:
					await _queueManager.ClearAsync(_queueName);
					break;
				case QueueOperation.READ:
					response = await _queueManager.ReadAsync(Constants.SINGLE_MESSAGE_COUNT, _queueName);
					break;
				case QueueOperation.READMANY:
					response = await _queueManager.ReadAsync(Constants.BATCH_MESSAGE_COUNT, _queueName);
					break;
				default:
					throw new NotImplementedException();
			}

			return response;
		}
	}
}
