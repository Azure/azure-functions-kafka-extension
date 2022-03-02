using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request
{
    public interface InvokeRequestStrategy<Response>
    {
        public Response InvokeRequest();
    }
}
