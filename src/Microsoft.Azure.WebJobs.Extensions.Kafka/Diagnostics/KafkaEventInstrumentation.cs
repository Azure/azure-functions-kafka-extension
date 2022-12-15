// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class KafkaEventInstrumentation
    {
        // For Trigger
        // Try to extract traceparent header
        public static void TryExtractTraceParentId(IKafkaEventData kafkaEvent, out string traceParentId)
        {
            traceParentId = null;

            if (kafkaEvent.Headers.TryGetFirst("traceparent", out byte[] traceParentIdInBytes))
            {
                traceParentId = Encoding.ASCII.GetString(traceParentIdInBytes);
            }
        }
    }
}
