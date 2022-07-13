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

            if (QueueOperation.CREATE == this.queueOperation)
            {
                await queueManager.createAsync(queueName);
            }
            else if (QueueOperation.CLEAR == this.queueOperation)
            {
                await queueManager.clearAsync(queueName);
            }
            else if (QueueOperation.DELETE == this.queueOperation)
            {
                await queueManager.deleteAsync(queueName);
            }
            else if (QueueOperation.READ == this.queueOperation)
            {
                response = await queueManager.readAsync(Constants.SINGLE_MESSAGE_COUNT, queueName);
            }
            else if (QueueOperation.READMANY == this.queueOperation)
            {
                response = await queueManager.readAsync(Constants.BATCH_MESSAGE_COUNT, queueName);
            }
            return response;
        }
    }
}
