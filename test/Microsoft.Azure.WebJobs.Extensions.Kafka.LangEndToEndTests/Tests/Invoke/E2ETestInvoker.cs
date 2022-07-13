using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke
{
    /* Responsible for actual invocation of function app depending on the passed strategy.
    */
    public class E2ETestInvoker
    {
        public async Task Invoke(InvokeRequestStrategy<HttpResponseMessage> invokeStrategy)
        {
            await invokeStrategy.InvokeRequestAsync();
        }

        public async Task Invoke(InvokeRequestStrategy<string> invokeStrategy)
        {
            await invokeStrategy.InvokeRequestAsync();
        }
    }
}
