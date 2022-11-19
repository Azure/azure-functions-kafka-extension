﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Diagnostics
{
    internal class SingleEventActivityProvider : ActivityProvider, IDisposable
    {
        private readonly IKafkaEventData kafkaEvent;

        private static string SingleKafkaTriggerActivityName { get; } = "SingleKafkaTrigger.Process";
        public SingleEventActivityProvider(IKafkaEventData kafkaEvent, string consumerGroup) : base(kafkaEvent.Topic, consumerGroup)
        {
            this.kafkaEvent = kafkaEvent;
        }

        public void CreateAndStartActivity()
        {
            KafkaEventInstrumentation.TryExtractTraceParentId(kafkaEvent, out string traceparent);
            this.CreateActivity(SingleKafkaTriggerActivityName, ActivityKind.Consumer, traceparent);
            this.AddActivityTags();
            this.StartActivity();
        }

        private new void AddActivityTags()
        {
            base.AddActivityTags();
            this.Activity?.AddTag(ActivityTags.KafkaPartition, kafkaEvent.Partition);
            this.Activity?.AddTag(ActivityTags.KafkaMessageKey, kafkaEvent.Key);
        }

        public void Dispose()
        {
            this.Activity?.Dispose();
        }
    }
}