using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation
{
    public enum QueueOperation
    {
        READ, READMANY, WRITE, CREATE, DELETE, CLEAR
    }
}
