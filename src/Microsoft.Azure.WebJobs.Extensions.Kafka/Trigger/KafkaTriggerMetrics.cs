// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaTriggerMetrics : ScaleMetrics
    {
        public long TotalLag { get; set; }
        public long PartitionCount { get; set; }
    }
}