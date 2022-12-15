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
        protected static readonly ActivitySource KafkaActivitySource = new ActivitySource("Microsoft.Azure.Webjobs.Extensions.Kafka");

        protected Activity activity;

        private readonly string kafkaTopicName;
        private readonly string consumerGroup;

        protected ActivityProvider(string kafkaTopicName, string consumerGroup)
        {
            this.kafkaTopicName = kafkaTopicName;
            this.consumerGroup = consumerGroup;
        }

        protected void CreateActivity(string name, ActivityKind kind, string traceparentId = null, List<ActivityLink> activityLinks = null)
        {
            this.activity = KafkaActivitySource.StartActivity(name, kind, traceparentId, null, activityLinks);
        }

        public void StartActivity()
        {
            this.activity?.Start();
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
        }
        public void SetActivityStatus(bool succeeded, Exception ex)
        {
            if (succeeded)
            {
                this.activity?.SetStatus(ActivityStatusCode.Ok, "");
            }
            else
            {
                this.activity?.SetStatus(ActivityStatusCode.Error, ex.Message.ToString());
            }
        }
    }
}
