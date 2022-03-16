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
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public async Task<QueueResponse> ExecuteCommandAsync()
        {
            if (QueueOperation.CREATE == this.queueOperation && QueueType.EventHub == queueType)
            {
                await queueManager.createAsync(queueName);
            }
            else if (QueueOperation.READ == this.queueOperation && QueueType.AzureStorageQueue == queueType)
            {
                await queueManager.readAsync(Constants.SINGLE_MESSAGE_COUNT, queueName);
            }
            else if (QueueOperation.READMANY == this.queueOperation && QueueType.AzureStorageQueue == queueType)
            {
                await queueManager.readAsync(Constants.BATCH_MESSAGE_COUNT, queueName);
            }
            throw new NotImplementedException();
        }
    }
}
