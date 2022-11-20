// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.Diagnostics;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class DiagnosticTest
    {
        [Fact]
        public void SingleActivityProvider_Should_Create_Activity()
        {
            var consumerGroup = "my-consumer-group";
            var numActivityTags = 7;
            var traceId = ActivityTraceId.CreateRandom();
            var spanId = ActivitySpanId.CreateRandom();
            string traceparent = "00-" + traceId + "-" + spanId + "-" + "01";
            var kafkaEvent = new KafkaEventData<string, string>("key", "value");
            kafkaEvent.Headers.Add("traceparent", Encoding.ASCII.GetBytes(traceparent));
            kafkaEvent.Topic = "mytopic";
            kafkaEvent.Partition = 1;

            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);
            var singleEventActivityProvider = new SingleEventActivityProvider(kafkaEvent, consumerGroup);
            Assert.NotNull(singleEventActivityProvider.Activity);
            var activity = singleEventActivityProvider.Activity;
            Assert.Equal(traceId, activity.TraceId);
            Assert.Equal(numActivityTags, activity.Tags.Count());
            Assert.Equal("kafka", activity.GetTagItem(ActivityTags.System).ToString());
            Assert.Equal(kafkaEvent.Topic, activity.GetTagItem(ActivityTags.DestinationName).ToString());
            Assert.Equal("topic", activity.GetTagItem(ActivityTags.DestinationKind).ToString());
            Assert.Equal("process", activity.GetTagItem(ActivityTags.Operation).ToString());
            Assert.Equal(consumerGroup, activity.GetTagItem(ActivityTags.KafkaConsumerGroup).ToString());
            Assert.Equal(kafkaEvent.Partition.ToString(), activity.GetTagItem(ActivityTags.KafkaPartition));
            var key = activity.GetTagItem(ActivityTags.KafkaMessageKey).ToString();
            Assert.Equal(kafkaEvent.Key, activity.GetTagItem(ActivityTags.KafkaMessageKey).ToString());
        }

        [Fact]
        public void BatchActivityProvider_Should_Create_Activity_With_Links()
        {
            var consumerGroup = "my-consumer-group";
            int numkafkaEvents = 10;
            var kafkaEvents = new KafkaEventData<string, string>[numkafkaEvents];
            var topicName = "mytopic";
            var numActivityTags = 5;
            for (int i = 0; i< numkafkaEvents; i++)
            {
                var traceId = ActivityTraceId.CreateRandom();
                var spanId = ActivitySpanId.CreateRandom();
                string traceparent = "00-" + traceId + "-" + spanId + "-" + "01";
                var kafkaEvent = new KafkaEventData<string, string>("key" + i, "value" + i);
                kafkaEvent.Headers.Add("traceparent", Encoding.ASCII.GetBytes(traceparent));
                kafkaEvent.Topic = topicName;
                kafkaEvent.Partition = 1;
                kafkaEvents[i] = kafkaEvent;
            }

            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);
            var batchEventActivityProvider = new BatchEventActivityProvider(kafkaEvents, consumerGroup);
            Assert.NotNull(batchEventActivityProvider.Activity);
            var activity = batchEventActivityProvider.Activity;
            var activityLinks = activity.Links.ToArray<ActivityLink>();

            Assert.Equal(kafkaEvents.Length, activityLinks.Length);

            for ( int i=0; i<kafkaEvents.Length; i++)
            {
                var kafkaEvent = kafkaEvents[i];
                kafkaEvent.Headers.TryGetFirst("traceparent", out var traceparentInBytes);
                var traceparent = Encoding.UTF8.GetString(traceparentInBytes);
                var traceId = traceparent.Split('-')[1];
                var spanId = traceparent.Split('-')[2];
                Assert.Equal(traceId, activityLinks[i].Context.TraceId.ToString());
                Assert.Equal(spanId, activityLinks[i].Context.SpanId.ToString());
            }

            Assert.Equal(numActivityTags, activity.Tags.Count());
            Assert.Equal("kafka", activity.GetTagItem(ActivityTags.System));
            Assert.Equal(topicName, activity.GetTagItem(ActivityTags.DestinationName));
            Assert.Equal("topic", activity.GetTagItem(ActivityTags.DestinationKind));
            Assert.Equal("process", activity.GetTagItem(ActivityTags.Operation));
            Assert.Equal(consumerGroup, activity.GetTagItem(ActivityTags.KafkaConsumerGroup));
        }
    }
}
