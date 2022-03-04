using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor
{
    public interface IExecutor<Request, Response>
    {
        public Task<Response> ExecuteAsync(Request request);
    }
}
