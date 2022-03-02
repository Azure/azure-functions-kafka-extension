using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor
{
    public interface IExecutor<Request, Response>
    {
        public Response Execute(Request request);
    }
}
