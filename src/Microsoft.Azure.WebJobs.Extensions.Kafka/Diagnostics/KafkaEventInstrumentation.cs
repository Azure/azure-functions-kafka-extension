// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Diagnostics
{
    internal static class KafkaEventInstrumentation
    {
        // For Trigger
        // Try to extract traceparent header
        public static bool TryExtractTraceParentId(IKafkaEventData kafkaEvent, out string traceParentId)
        {
            traceParentId = null;

            if (kafkaEvent.Headers.TryGetFirst("traceparent", out byte[] traceParentIdInBytes))
            {
                traceParentId = Encoding.ASCII.GetString(traceParentIdInBytes);
                return true;
            }
            return false;
        }

        public static List<ActivityLink> CreateLinkedActivities(IKafkaEventData[] kafkaEvents)
        {
            var activityLinks = new List<ActivityLink>();
            foreach (var kafkaEvent in kafkaEvents)
            {
                kafkaEvent.Headers.TryGetFirst("traceparent", out byte[] traceParentIdInBytes);
                var traceParentId = Encoding.ASCII.GetString(traceParentIdInBytes);
                var link = ActivityHelper.CreateActivityLink(traceParentId);
                activityLinks.Add(link);
            }
            return activityLinks;
        }
    }
}
