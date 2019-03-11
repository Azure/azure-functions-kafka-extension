using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public interface IConsumeResultData
    {
        object Key { get; }

        //
        // Summary:
        //     The topic associated with the message.
        string Topic { get; }
        //
        // Summary:
        //     The partition associated with the message.
        int Partition { get; }
        //
        // Summary:
        //     The partition offset associated with the message.
        long Offset { get; }

        DateTime Timestamp { get; }

        object Value { get; }
    }
}