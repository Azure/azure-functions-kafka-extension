using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.Type
{
    /* Determines the type of invocation required to trigger the function app
    */
    public enum InvokeType
    {
        HTTP,
        Kafka
    }
}
