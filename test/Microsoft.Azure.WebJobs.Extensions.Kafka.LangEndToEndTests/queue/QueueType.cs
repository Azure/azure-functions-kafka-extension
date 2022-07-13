using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
    /* External resources required for the test are abstracted as queues.
     * Collection of possible queue types.
    */
    public enum QueueType
    {
        EventHub,
        AzureStorageQueue,
        Kafka
    }
}
