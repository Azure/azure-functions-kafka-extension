// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTriggerMetrics : ScaleMetrics
    {
        public long TotalLag { get; set; }
        public long PartitionCount { get; set; }

        public KafkaTriggerMetrics()
        {
        }

        public KafkaTriggerMetrics(long totalLag, int partitionCount)
        {
            TotalLag = totalLag;
            PartitionCount = partitionCount;
            Timestamp = DateTime.UtcNow;
        }
    }
}