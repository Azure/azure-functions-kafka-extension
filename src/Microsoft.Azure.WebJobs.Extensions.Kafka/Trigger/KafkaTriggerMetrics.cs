// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTriggerMetrics : ScaleMetrics
    {
        /// <summary>
        /// The total lag accross all partitions.
        /// </summary>
        public long TotalLag { get; set; }
        
        /// <summary>
        /// The number of partitions.
        /// </summary>
        public long PartitionCount { get; set; }

        public KafkaTriggerMetrics(long totalLag, int partitionCount, long eventCount = 0)
        {
            TotalLag = totalLag;
            PartitionCount = partitionCount;
        }
    }
}