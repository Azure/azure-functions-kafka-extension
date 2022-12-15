// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Executors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class ActivityProvider
    {
        public static readonly ActivitySource ActivitySource = new ActivitySource("Microsoft.Azure.Webjobs.Extensions.Kafka");

        public Activity Activity;

        private readonly string kafkaTopicName;
        private readonly string consumerGroup;

        protected ActivityProvider(string kafkaTopicName, string consumerGroup)
        {
            this.kafkaTopicName = kafkaTopicName;
            this.consumerGroup = consumerGroup;
        }

        protected void CreateActivity(string name, ActivityKind kind, string traceparentId=null, List<ActivityLink> activityLinks=null)
        {
            this.Activity = ActivitySource.StartActivity(name, kind, traceparentId, null, activityLinks);
        }

        protected void StartActivity()
        {
            this.Activity?.Start();
        }

        public void SetActivityStatusSucceded()
        {
            this.Activity?.SetStatus(ActivityStatusCode.Ok, "");
        }

        public void SetActivityStatusError(Exception ex)
        {
            this.Activity?.SetStatus(ActivityStatusCode.Error, ex.Message.ToString());
        }

        public void StopCurrentActivity()
        {
           this.Activity?.Stop();
        }

        protected void AddActivityTags()
        {
            this.Activity?.AddTag(ActivityTags.System, "kafka");
            this.Activity?.AddTag(ActivityTags.DestinationName, this.kafkaTopicName);
            this.Activity?.AddTag(ActivityTags.DestinationKind, "topic");
            this.Activity?.AddTag(ActivityTags.Operation, "process");
            this.Activity?.AddTag(ActivityTags.KafkaConsumerGroup, this.consumerGroup);
            //this.Activity?.AddTag(ActivityTags.KafkaClientId, kafkaClientId);
        }
    }
}
