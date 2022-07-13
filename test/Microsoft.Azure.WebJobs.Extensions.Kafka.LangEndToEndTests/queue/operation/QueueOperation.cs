using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation
{
    /* Defines possible operation that can be performed on the Queue abstraction.
    */
    public enum QueueOperation
    {
        READ, READMANY, WRITE, CREATE, DELETE, CLEAR
    }
}
