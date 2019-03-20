// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public interface IConsumeResultData
    {
        /// <summary>
        /// The message key
        /// </summary>
        object Key { get; }

        /// <summary>
        /// The topic associated with the message.
        /// </summary>
        string Topic { get; }

        /// <summary>
        /// The partition associated with the message.
        /// </summary>
        int Partition { get; }

        /// <summary>
        /// The partition offset associated with the message.
        /// </summary>
        long Offset { get; }

        /// <summary>
        /// The message timestamp
        /// </summary>
        DateTime Timestamp { get; }
        
        /// <summary>
        /// The message content
        /// </summary>
        object Value { get; }
    }
}