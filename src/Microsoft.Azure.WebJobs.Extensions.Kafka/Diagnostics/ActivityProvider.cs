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
        private static readonly ActivitySource KafkaActivitySource = new ActivitySource("Microsoft.Azure.Webjobs.Extensions.Kafka");

        private Activity activity;

        private readonly string kafkaTopicName;
        private readonly string consumerGroup;

        protected ActivityProvider(string kafkaTopicName, string consumerGroup)
        {
            this.kafkaTopicName = kafkaTopicName;
            this.consumerGroup = consumerGroup;
        }

        protected void CreateActivity(string name, ActivityKind kind, string traceparentId=null, List<ActivityLink> activityLinks=null)
        {
            this.activity = KafkaActivitySource.StartActivity(name, kind, traceparentId, null, activityLinks);
        }

        protected void StartActivity()
        {
            this.activity?.Start();
        }

        public void SetActivityStatusSucceded()
        {
            this.activity?.SetStatus(ActivityStatusCode.Ok, "");
        }

        public void SetActivityStatusError(Exception ex)
        {
            this.activity?.SetStatus(ActivityStatusCode.Error, ex.Message.ToString());
        }

        public void StopCurrentActivity()
        {
           this.activity?.Stop();
        }

        protected void AddActivityTags()
        {
            this.activity?.AddTag(ActivityTags.System, "kafka");
            this.activity?.AddTag(ActivityTags.DestinationName, this.kafkaTopicName);
            this.activity?.AddTag(ActivityTags.DestinationKind, "topic");
            this.activity?.AddTag(ActivityTags.Operation, "process");
            this.activity?.AddTag(ActivityTags.KafkaConsumerGroup, this.consumerGroup);
            //this.Activity?.AddTag(ActivityTags.KafkaClientId, kafkaClientId);
        }
    }
}
