using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor
{
    /* Interface for all Request Executors that return Response async
    */
    public interface IExecutor<Request, Response>
    {
        public Task<Response> ExecuteAsync(Request request);
    }
}
