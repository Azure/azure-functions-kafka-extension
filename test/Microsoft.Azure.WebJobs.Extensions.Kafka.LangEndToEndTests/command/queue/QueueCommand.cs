// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue
{
    /* Responsible for creation and execution of commands to interact with Queue Type resources(External Resources).
    */
    public class QueueCommand : Command<QueueResponse>, IDisposable
    {
        private QueueType queueType;
        private QueueOperation queueOperation;
        private string queueName;
        private IQueueManager<QueueRequest, QueueResponse> queueManager;
        
        public QueueCommand(QueueType queueType, QueueOperation queueOperation, string queueName)
        {
            this.queueType = queueType;
            this.queueName = queueName;
            this.queueOperation = queueOperation;
            if (QueueType.EventHub == queueType)
            {
                this.queueManager = EventHubQueueManager.GetInstance();
            }
            else if (QueueType.AzureStorageQueue == queueType)
            {
                this.queueManager = AzureStorageQueueManager.GetInstance();
            }
        }
        public void Dispose() { }

        public async Task<QueueResponse> ExecuteCommandAsync()
        {
            QueueResponse response = null;

            switch (queueOperation)
            {
                case QueueOperation.CREATE:
                    await queueManager.CreateAsync(queueName);
                    break;
                case QueueOperation.DELETE:
                    await queueManager.DeleteAsync(queueName);
                    break;
                case QueueOperation.CLEAR:
                    await queueManager.ClearAsync(queueName);
					break;
                case QueueOperation.READ:
                    response = await queueManager.ReadAsync(Constants.SINGLE_MESSAGE_COUNT, queueName);
                    break;
                case QueueOperation.READMANY:
                    response = await queueManager.ReadAsync(Constants.BATCH_MESSAGE_COUNT, queueName);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return response;
        }
    }
}
