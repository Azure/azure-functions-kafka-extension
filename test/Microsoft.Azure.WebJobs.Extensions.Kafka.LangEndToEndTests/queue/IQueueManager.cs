using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
  
    public interface IQueueManager<Request, Response>
    {
        public Task<Response> readAsync(int batchSize);
        public Task<Response> writeAsync(Request messageEntity);
        public Task createAsync(string queueName);
        public Task deleteAsync(string queueName);
        public Task clearAsync(string queueName);
    }
}
