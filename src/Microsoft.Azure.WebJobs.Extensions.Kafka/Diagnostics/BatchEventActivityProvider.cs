// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class BatchEventActivityProvider : ActivityProvider, IDisposable
    {
        private List<ActivityLink> activityLinks;
        private readonly IKafkaEventData[] kafkaEvents;
        private static string KafkaBatchTriggerActivityName { get; } = "MultipleKafkaTrigger.Process";

        public BatchEventActivityProvider(IKafkaEventData[] kafkaEvents, string consumerGroup) : base(GetTopic(kafkaEvents), consumerGroup)
        {
            this.kafkaEvents = kafkaEvents;
            this.activityLinks = new List<ActivityLink>();
            this.CreateActivity();
        }

        private static string GetTopic(IKafkaEventData[] kafkaEvents)
        {
            if (kafkaEvents.Length == 0)
            {
                throw new Exception("KafkaEvents array is null");
            }
            return kafkaEvents[0].Topic;
        }

        public void CreateActivity()
        {
            if (KafkaActivitySource.HasListeners())
            {
                this.CreateActivityLinksForAllEvents();
                this.CreateActivity(KafkaBatchTriggerActivityName, ActivityKind.Consumer, null, activityLinks);
                this.AddActivityTags();
            }
        }

        public void CreateActivityLink(string traceParentId)
        {
            var traceParentFields = traceParentId.Split('-');
            if (traceParentFields.Length != 4)
            {
                //ERROR: Invalid traceparent Header
            }

            var traceId = ActivityTraceId.CreateFromString(traceParentFields[1].AsSpan());
            var spanId = ActivitySpanId.CreateFromString(traceParentFields[2].AsSpan());
            var linkedContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.None);
            var link = new ActivityLink(linkedContext);
            activityLinks.Add(link);
        }

        private void CreateActivityLinksForAllEvents()
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                KafkaEventInstrumentation.TryExtractTraceParentId(kafkaEvent, out var traceParentId);
                this.CreateActivityLink(traceParentId);
            }
        }

        public void Dispose()
        {
            this.activity?.Dispose();
            this.activityLinks.Clear();
        }
    }
}
