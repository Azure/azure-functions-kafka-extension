using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke
{
    public class E2ETestInvoker
    {
        public void Invoke(InvokeRequestStrategy<HttpResponse> invokeStrategy)
        {
            invokeStrategy.InvokeRequest();
        }

        public void Invoke(InvokeRequestStrategy<string> invokeStrategy)
        {
            invokeStrategy.InvokeRequest();
        }
    }
}
