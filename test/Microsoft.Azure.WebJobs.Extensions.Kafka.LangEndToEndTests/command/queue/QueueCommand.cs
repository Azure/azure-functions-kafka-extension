using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue
{
    public class QueueCommand : Command<String>, IDisposable
    {
        private QueueType queueType;
        private QueueOperation queueOperation;
        private IQueueManager<string, string> queueManager;
        //private IQueueManager<List<string>, List<string>> azureStorageQueueManager;
        
        public QueueCommand(QueueType queueType, QueueOperation queueOperation)
        {
            this.queueType = queueType;
            this.queueManager = null;
            this.queueOperation = queueOperation;
            if(QueueType.EventHub == queueType)
            {
                this.queueManager = EventHubQueueManager.GetInstance();

            }

        }
        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public async Task<String> ExecuteCommandAsync()
        {
            if(QueueOperation.CREATE == this.queueOperation && QueueType.EventHub == queueType)
            {
                //queueManager.create(queueName);
            }
            throw new NotImplementedException();
        }
    }
}
