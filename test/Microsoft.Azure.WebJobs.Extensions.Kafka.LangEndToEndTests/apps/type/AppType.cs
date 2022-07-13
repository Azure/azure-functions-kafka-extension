using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type
{
    /* Represents the app types supported in kafka extension - 
     * Single Mode consumes one message at a time
     * Batch Mode consumes a batch of message for processing at a time
    */
    public enum AppType
    {
        SINGLE_EVENT,
        BATCH_EVENT
    }
}
