using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
    public enum QueueType
    {
        EventHub,
        AzureStorageQueue,
        Kafka
    }
}
