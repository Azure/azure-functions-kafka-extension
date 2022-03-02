using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
    public interface IQueueManager<Request, Response>
    {
        public Response read(int batchSize);
        public Response write(Request messageEntity);
        public void create(string queueName);
        public void delete(string queueName);
        public void clear(string queueName);
    }
}
