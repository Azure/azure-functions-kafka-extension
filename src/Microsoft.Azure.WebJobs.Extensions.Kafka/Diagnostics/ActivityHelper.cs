// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Executors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Diagnostics
{
    internal static class ActivityHelper
    {
        private static readonly ActivitySource activitySource = new ActivitySource("Microsoft.Azure.Webjobs.Extensions.Kafka");

        // Todo: Decide for the correct activity name
        public static string KafkaTriggerActivityName { get; } = "KafkaTrigger.Process";

        public static Activity StartActivityForProcessing(string traceparentId=null, List<KeyValuePair<string, object>> tags=null, List<ActivityLink> activityLinks=null)
        {
            var activity = activitySource.StartActivity(KafkaTriggerActivityName, ActivityKind.Consumer, traceparentId, tags, activityLinks);
            return activity;
        }

        public static void SetActivityStatus(bool succeeded, Exception ex)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                if (succeeded)
                {
                    activity.SetStatus(ActivityStatusCode.Ok, "");
                }
                else
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message.ToString());
                }
            }
        }

        public static void StopCurrentActivity()
        {
            Activity.Current?.Stop();
        }

        public static ActivityLink CreateActivityLink(string traceParentId)
        {
            var traceParentFields = traceParentId.Split('-');

            var linkedContext = new ActivityContext(ActivityTraceId.CreateFromString(traceParentFields[1].AsSpan()),
                                                      ActivitySpanId.CreateFromString(traceParentFields[2].AsSpan()),
                                                      ActivityTraceFlags.None);
            ActivityLink activityLink = new ActivityLink(linkedContext);
            return activityLink;
        }
    }
}
