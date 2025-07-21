// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Tests;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    [Trait("Category", "E2E")]
    public class KafkaEndToEndTests : IClassFixture<KafkaEndToEndTestFixture>
    {
        private readonly TestLoggerProvider loggerProvider;
        private readonly KafkaEndToEndTestFixture endToEndTestFixture;

        internal static TestLoggerProvider CreateTestLoggerProvider()
        {
            return (System.Diagnostics.Debugger.IsAttached) ?
                new TestLoggerProvider((l) => System.Diagnostics.Debug.WriteLine(l.ToString())) :
                new TestLoggerProvider();
        }

        public KafkaEndToEndTests(KafkaEndToEndTestFixture endToEndTestFixture)
        {
            loggerProvider = CreateTestLoggerProvider();
            this.endToEndTestFixture = endToEndTestFixture;
        }

        [Fact]
        public async Task StringValue_SingleTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(SingleItem_Single_Partition_Raw_String_Without_Key_Trigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch1));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);
            }

            var loggerProvider2 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(SingleItem_Single_Partition_Raw_String_Without_Key_Trigger), typeof(KafkaOutputFunctions) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider2.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch2));
                    return foundCount == producedMessagesCount;
                });
            }

            // Ensure 2 run does not have any item from previous run
            Assert.DoesNotContain(loggerProvider2.GetAllUserLogMessages().Where(p => p.FormattedMessage != null).Select(x => x.FormattedMessage), x => x.Contains(messagePrefixBatch1));
        }

        private MethodInfo GetStaticMethod(Type type, string methodName) => type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);

        [Fact]
        public async Task SinglePartition_StringValue_ArrayTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(MultiItem_KafkaEventData_String_Without_Key_Trigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch1));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var loggerProvider2 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItem_KafkaEventData_String_Without_Key_Trigger) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider2.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch2));
                    return foundCount == producedMessagesCount;
                });

                await host.StopAsync();
            }

            // Ensure 2 run does not have any item from previous run
            Assert.DoesNotContain(loggerProvider2.GetAllUserLogMessages().Where(p => p.FormattedMessage != null).Select(x => x.FormattedMessage), x => x.Contains(messagePrefixBatch1));
        }

        [Fact]
        public async Task SinglePartition_StringValue_ByteArrayTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(MultiItem_RawByteArray_Trigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch1));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var loggerProvider2 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItem_RawByteArray_Trigger) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider2.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch2));
                    return foundCount == producedMessagesCount;
                });

                await host.StopAsync();
            }

            // Ensure 2 run does not have any item from previous run
            Assert.DoesNotContain(loggerProvider2.GetAllUserLogMessages().Where(p => p.FormattedMessage != null).Select(x => x.FormattedMessage), x => x.Contains(messagePrefixBatch1));
        }

        [Fact]
        public async Task SinglePartition_StringValue_ByteArrayTriggerOneItem_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(SingleItem_SinglePartition_RawByteArray_Trigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch1));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var loggerProvider2 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(SingleItem_SinglePartition_RawByteArray_Trigger) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider2.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch2));
                    return foundCount == producedMessagesCount;
                });

                await host.StopAsync();
            }

            // Ensure 2 run does not have any item from previous run
            Assert.DoesNotContain(loggerProvider2.GetAllUserLogMessages().Where(p => p.FormattedMessage != null).Select(x => x.FormattedMessage), x => x.Contains(messagePrefixBatch1));
        }

        [Fact]
        public async Task SinglePartition_StringValue_SingleTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(SingleItem_KafkaEventData_String_Without_Key_Trigger) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithTenPartitions.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch1));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var loggerProvider2 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(SingleItem_KafkaEventData_String_Without_Key_Trigger) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithTenPartitions.Name,
                    Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider2.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch2));
                    return foundCount == producedMessagesCount;
                });

                await host.StopAsync();
            }

            // Ensure 2 run does not have any item from previous run
            Assert.DoesNotContain(loggerProvider2.GetAllUserLogMessages().Where(p => p.FormattedMessage != null).Select(x => x.FormattedMessage), x => x.Contains(messagePrefixBatch1));
        }

        [Fact]
        public async Task MultiPartition_StringValue_ArrayTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItem_String_Without_Key_Trigger) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithTenPartitions.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch1));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var loggerProvider2 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItem_String_Without_Key_Trigger) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithTenPartitions.Name,
                    Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider2.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefixBatch2));
                    return foundCount == producedMessagesCount;
                });

                await host.StopAsync();
            }

            // Ensure 2 run does not have any item from previous run
            Assert.DoesNotContain(loggerProvider2.GetAllUserLogMessages().Where(p => p.FormattedMessage != null).Select(x => x.FormattedMessage), x => x.Contains(messagePrefixBatch1));
        }

        /// <summary>
        /// Ensures that multiple hosts processing a topic with 10 partition share the content, having the events being processed at least once.
        /// 
        /// Test flow:
        /// 1. In a separated task producer creates 4x80 items. After the first batch is created waits for semaphore.
        /// 2. In main task host1 starts processing messages
        /// 3. When host1 has at least a message starts hosts2
        /// 4. When host2 obtains at least 1 partitions it triggers the semaphore
        /// 5. Once the producer tasks is finished (all 240 messages were created), validate that all messages were processed by host1 and host2
        /// </summary>
        [Fact]
        public async Task Multiple_Hosts_Process_Events_At_Least_Once()
        {
            const int producedMessagesCount = 240;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            var producerHost = await StartHostAsync(typeof(KafkaOutputFunctions));
            var producerJobHost = producerHost.GetJobHost();

            var host2HasPartitionsSemaphore = new SemaphoreSlim(0);

            // Split the call in 4, waiting 1sec between calls
            var producerTask = Task.Run(async () =>
            {
                var allMessages = Enumerable.Range(1, producedMessagesCount).Select(x => EndToEndTestExtensions.CreateMessageValue(messagePrefix, x));
                const int loopCount = 4;
                var itemsPerLoop = producedMessagesCount / loopCount;
                for (var i = 0; i < loopCount; ++i)
                {
                    var messages = allMessages.Skip(i * itemsPerLoop).Take(itemsPerLoop);
                    await producerJobHost.CallOutputTriggerStringAsync(
                        GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                        this.endToEndTestFixture.StringTopicWithTenPartitions.Name,
                        messages);

                    if (i == 0)
                    {
                        // wait until host2 has partitions assigned
                        Assert.True(await host2HasPartitionsSemaphore.WaitAsync(TimeSpan.FromSeconds(50)), "Host2 has not been assigned any partition after waiting for 30 seconds");
                    }

                    await Task.Delay(100);
                }
            });

            IHost host1 = null, host2 = null;
            Func<LogMessage, bool> messageFilter = (LogMessage m) => m.FormattedMessage != null && m.FormattedMessage.Contains(messagePrefix);

            try
            {
                var host1Log = CreateTestLoggerProvider();
                var host2Log = CreateTestLoggerProvider();

                host1 = await StartHostAsync(typeof(MultiItem_String_Without_Key_Trigger), host1Log);

                // wait until host1 receives partitions
                await TestHelpers.Await(() =>
                {
                    var host1HasPartitions = host1Log.GetAllLogMessages().Any(x => x.FormattedMessage != null && x.FormattedMessage.Contains("Assigned partitions"));
                    return host1HasPartitions;
                });


                host2 = await StartHostAsync(typeof(MultiItem_String_Without_Key_Trigger), host2Log);

                // wait until partitions are distributed
                await TestHelpers.Await(() =>
                {
                    var host2HasPartitions = host2Log.GetAllLogMessages().Any(x => x.FormattedMessage != null && x.FormattedMessage.Contains("Assigned partitions"));

                    if (host2HasPartitions)
                    {
                        host2HasPartitionsSemaphore.Release();
                    }

                    return host2HasPartitions;
                });

                // Wait until producer is finished
                await producerTask;

                await TestHelpers.Await(() =>
                {
                    var host1Events = host1Log.GetAllUserLogMessages().Where(messageFilter).Select(x => x.FormattedMessage).ToList();
                    var host2Events = host2Log.GetAllUserLogMessages().Where(messageFilter).Select(x => x.FormattedMessage).ToList();

                    return host1Events.Count > 0 &&
                        host2Events.Count > 0 &&
                        host2Events.Count + host1Events.Count >= producedMessagesCount;
                });


                await TestHelpers.Await(() =>
                {
                    // Ensure every message was processed at least once
                    var allLogs = new List<string>(host1Log.GetAllLogMessages().Where(messageFilter).Select(x => x.FormattedMessage));
                    allLogs.AddRange(host2Log.GetAllLogMessages().Where(messageFilter).Select(x => x.FormattedMessage));

                    for (int i = 1; i <= producedMessagesCount; i++)
                    {
                        var currentMessage = EndToEndTestExtensions.CreateMessageValue(messagePrefix, i);
                        var count = allLogs.Count(x => x == currentMessage);
                        if (count == 0)
                        {
                            return false;
                        }
                    }

                    return true;
                });

                // For history write down items that have been processed more than once
                // If an item is processed more than 2x times test fails
                var logs = new List<string>(host1Log.GetAllLogMessages().Where(messageFilter).Select(x => x.FormattedMessage));
                logs.AddRange(host2Log.GetAllLogMessages().Where(messageFilter).Select(x => x.FormattedMessage));

                var multipleProcessItemCount = 0;
                for (int i = 1; i <= producedMessagesCount; i++)
                {
                    var currentMessage = EndToEndTestExtensions.CreateMessageValue(messagePrefix, i);
                    var count = logs.Count(x => x == currentMessage);
                    if (count > 1)
                    {
                        Assert.True(count < 3, $"{currentMessage} was processed {count} times");
                        multipleProcessItemCount++;
                        Console.WriteLine($"{currentMessage} was processed {count} times");
                    }
                }

                // Should not process more than 10% of all items a second time.
                Assert.InRange(multipleProcessItemCount, 0, producedMessagesCount / 10);
            }
            finally
            {
                await host1?.StopAsync();
                await host2?.StopAsync();
            }

            await producerTask;
            await producerHost?.StopAsync();

        }

        private List<KafkaEventData<string>> GetKafkaEventsWithTraceParentHeader(int numEvents)
        {
            var input = Enumerable.Range(1, numEvents)
                .Select(x => {
                    var eventData = new KafkaEventData<string>
                    {
                        Value = x.ToString()
                    };

                    for (var i = 0; i < x; i++)
                    {
                        var traceId = ActivityTraceId.CreateRandom();
                        var spanId = ActivitySpanId.CreateRandom();
                        string traceparent = "00-" + traceId + "-" + spanId + "-" + "01";
                        eventData.Headers.Add("traceparent", Encoding.UTF8.GetBytes(traceparent));
                    }
                    return eventData;
                });
            return input.ToList();
        }

        private List<KafkaEventData<string>> GetKafkaEventsWithoutTraceParentHeader(int numEvents)
        {
            var input = Enumerable.Range(1, numEvents)
                .Select(x => {
                    return new KafkaEventData<string>
                    {
                        Value = x.ToString()
                    };
                });
            return input.ToList();
        }

        [Fact]
        public async Task Tracing_For_SingleEventTrigger()
        {
            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);

            int producedMessagesCount = 10;


            var loggerProvider = CreateTestLoggerProvider();

            var eventList = GetKafkaEventsWithTraceParentHeader(producedMessagesCount);
            foreach (var kafkaEvent in eventList)
            {
                kafkaEvent.Topic = Constants.StringTopicWithOnePartitionName;
            }
            var eventCount = eventList.Count;

            using (var host = await StartHostAsync(new[] {typeof(SingleEventTrigger_With_Activity), typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>) }, loggerProvider,
                serviceRegistrationCallback: s =>
                {
                    s.AddSingleton(eventList);
                }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallAsync(
                    typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>).GetMethod(nameof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>.Produce))
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains("TraceId:"));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }
            
            foreach(var kafkaEvent in eventList)
            {
                var traceparent = Encoding.ASCII.GetString(kafkaEvent.Headers.GetFirst("traceparent"));
                var traceId = traceparent.Split('-')[1];
                var message = "TraceId:" + traceId;
                Assert.Single(loggerProvider.GetAllUserLogMessages().Where(p => p.FormattedMessage != null && p.FormattedMessage.Contains(message)));
            }

            activityListener.Dispose();
        }

        [Fact]
        public async Task Tracing_SingleEventTrigger_With_No_Traceparent() 
        {
            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);

            int producedMessagesCount = 10;


            var loggerProvider = CreateTestLoggerProvider();

            var eventList = GetKafkaEventsWithoutTraceParentHeader(producedMessagesCount);
            foreach (var kafkaEvent in eventList)
            {
                kafkaEvent.Topic = Constants.StringTopicWithOnePartitionName;
            }
            var eventCount = eventList.Count;

            using (var host = await StartHostAsync(new[] { typeof(SingleEventTrigger_With_Activity), typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>) }, loggerProvider,
                serviceRegistrationCallback: s =>
                {
                    s.AddSingleton(eventList);
                }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallAsync(
                    typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>).GetMethod(nameof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>.Produce))
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains("TraceId:"));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var message = "TraceId:";
            var loggedMessages = loggerProvider.GetAllUserLogMessages().Where(p => p.FormattedMessage != null && p.FormattedMessage.Contains(message));
            Assert.Equal(loggedMessages.Count(), eventCount);
            foreach ( var msg in loggedMessages)
            {
                var traceparent = msg.FormattedMessage.Split(":")[1];
                Assert.NotNull(traceparent);
            }

            activityListener.Dispose();
        }

        [Fact]
        public async Task Tracing_For_BatchEventTrigger()
        {
            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);

            int producedMessagesCount = 10;


            var loggerProvider = CreateTestLoggerProvider();

            var eventList = GetKafkaEventsWithTraceParentHeader(producedMessagesCount);
            foreach (var kafkaEvent in eventList)
            {
                kafkaEvent.Topic = Constants.StringTopicWithOnePartitionName;
            }
            var eventCount = eventList.Count;

            using (var host = await StartHostAsync(new[] { typeof(BatchEvenTrigger_With_Activity), typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>) }, loggerProvider,
                serviceRegistrationCallback: s =>
                {
                    s.AddSingleton(eventList);
                }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallAsync(
                    typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>).GetMethod(nameof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>.Produce))
                    );
                        
                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains("TraceId:"));
                    return foundCount == 1;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            foreach (var kafkaEvent in eventList)
            {
                var traceparent = Encoding.ASCII.GetString(kafkaEvent.Headers.GetFirst("traceparent"));
                var message = "LinkedActivity: " + traceparent;
                Assert.Single(loggerProvider.GetAllUserLogMessages().Where(p => p.FormattedMessage != null && p.FormattedMessage.Contains(message)));
            }

            activityListener.Dispose();
        }

        [Fact]
        public async Task Tracing_For_BatchEventTrigger_Without_Traceparent()
        {
            var activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };
            ActivitySource.AddActivityListener(activityListener);

            int producedMessagesCount = 10;


            var loggerProvider = CreateTestLoggerProvider();

            var eventList = GetKafkaEventsWithoutTraceParentHeader(producedMessagesCount);
            foreach (var kafkaEvent in eventList)
            {
                kafkaEvent.Topic = Constants.StringTopicWithOnePartitionName;
            }
            var eventCount = eventList.Count;

            using (var host = await StartHostAsync(new[] { typeof(BatchEvenTrigger_With_Activity), typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>) }, loggerProvider,
                serviceRegistrationCallback: s =>
                {
                    s.AddSingleton(eventList);
                }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallAsync(
                    typeof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>).GetMethod(nameof(KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>.Produce))
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains("TraceId:"));
                    return foundCount == 1;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }

            var message = "LinkedActivity: ";
            Assert.Empty(loggerProvider.GetAllUserLogMessages().Where(p => p.FormattedMessage != null && p.FormattedMessage.Contains(message)));
            activityListener.Dispose();
        }


        [Theory]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_Out_Parameter_KafkaEventData_Array_String_Without_Key),
            typeof(SingleItem_Raw_String_Without_Key_Trigger),
            Constants.StringTopicWithTenPartitionsName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_Return_Parameter_Raw_String_Array),
            typeof(MultiItem_Raw_StringArray_Without_Key_Trigger),
            Constants.StringTopicWithTenPartitionsName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncColletor_Raw_String_Without_Key),
            typeof(MultiItem_KafkaEventData_String_With_Ignore_Key_Trigger),
            Constants.StringTopicWithTenPartitionsName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncColletor_Raw_ByteArray_Without_Key),
            typeof(SingleItem_RawByteArray_Trigger),
            Constants.StringTopicWithTenPartitionsName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncCollector_Raw_SpecificAvro),
            typeof(MultiItem_Raw_SpecificAvro_Without_Key_Trigger),
            Constants.MyAvroRecordTopicName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_Return_Parameter_Raw_Protobuf_Without_Key),
            typeof(MultiItem_Raw_Protobuf_Trigger),
            Constants.MyProtobufTopicName)]
        public async Task Produce_And_Consume_Without_Key(string producerFunctionName, Type triggerFunctionType, string topicName)
        {
            const int producedMessagesCount = 20;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), triggerFunctionType }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), producerFunctionName),
                    topicName,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefix + x)
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1000);
            }
        }

        [Theory]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_With_Long_Key),
            typeof(MultiItem_KafkaEventData_String_With_Long_Key_Trigger),
            Constants.StringTopicWithLongKeyAndTenPartitionsName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_Out_Parameter_KafkaEventData_Array_String_With_String_Key),
            typeof(MultiItem_KafkaEventData_String_With_String_Key_Trigger),
            Constants.StringTopicWithTenPartitionsName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncCollector_Avro_With_String_key),
            typeof(MultiItem_SpecificAvro_With_String_Key_Trigger),
            Constants.MyAvroRecordTopicName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncCollector_Avro_With_String_key),
            typeof(MultiItem_GenericAvro_With_String_Key_Trigger),
            Constants.MyAvroRecordTopicName)]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_AsyncCollector_Protobuf_With_String_Key),
            typeof(MultiItem_Protobuf_With_String_Key_Trigger),
            Constants.MyProtobufTopicName
            )]
        [InlineData(
            nameof(KafkaOutputFunctions.Produce_Return_Parameter_KafkaEventData_Array_String_With_String_Key),
            typeof(MultiItem_RawStringArray_Trigger),
            Constants.StringTopicWithTenPartitionsName)]
        public async Task Produce_And_Consume_With_Key(string producerFunctionName, Type triggerFunctionType, string topicName)
        {
            const int producedMessagesCount = 20;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), triggerFunctionType }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringWithKeyAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), producerFunctionName),
                    topicName,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefix + x),
                    Enumerable.Range(1, producedMessagesCount).Select(x => x.ToString())
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1000);
            }
        }

        [Fact]
        public async Task Produce_And_Consume_With_Headers()
        {
            var input = Enumerable.Range(1, 10)
                .Select(x => {
                    var eventData = new KafkaEventData<string>
                    {
                        Value = x.ToString()
                    };

                    for (var i = 0; i < x; i++)
                    {
                        eventData.Headers.Add("testHeader", Encoding.UTF8.GetBytes("testValue" + i));
                    }
                    return eventData;
                });

            var output = await ProduceAndConsumeAsync<KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>, KafkaTriggerForProduceAndConsume<KafkaEventData<string>>>(x => x.Produce(default), input);

            foreach (var inputEvent in input)
            {
                var outputEvent = output.SingleOrDefault(x => x.Value == inputEvent.Value);

                Assert.NotNull(outputEvent);
                Assert.Equal(inputEvent.Headers.Count, outputEvent.Headers.Count);
                Assert.Equal("testValue0", Encoding.UTF8.GetString(outputEvent.Headers.GetFirst("testHeader")));
                Assert.Throws<NotSupportedException>(() => outputEvent.Headers.Remove("testHeader"));
            }
        }

        [Fact]
        public async Task Produce_And_Consume_Without_Headers()
        {
            var input = Enumerable.Range(0, 10)
                .Select(x => new KafkaEventData<string>
                {
                    Value = x.ToString()
                });

            var output = await ProduceAndConsumeAsync<KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>, KafkaTriggerForProduceAndConsume<KafkaEventData<string>>>(x => x.Produce(default), input.ToArray());

            foreach (var inputEvent in input)
            {
                var outputEvent = output.SingleOrDefault(x => x.Value == inputEvent.Value);

                Assert.NotNull(outputEvent);
                Assert.Equal(0, outputEvent.Headers.Count);

                //All events should have the same headers instance
                Assert.Same(outputEvent.Headers, output.First().Headers);
                Assert.Throws<NotSupportedException>(() => outputEvent.Headers.Remove("testHeader"));
            }
            
        }

        [Fact]
        public async Task Produce_With_MaxMessageBytes_Should_Throw_Error_When_Message_Is_Too_Large()
        {
            var input = new[] {
                    new KafkaEventData<string>
                    {
                        Value = new string('a', 1000)
                    }
                };

            var ex = await Assert.ThrowsAsync<FunctionInvocationException>(async () => await ProduceAndConsumeAsync<KafkaOutputFunctionsForProduceAndConsume<KafkaEventData<string>>, KafkaTriggerForProduceAndConsume<KafkaEventData<string>>>(x => x.ProduceWithMaxMessageBytes1000(default), input));
            Assert.NotNull(ex.InnerException);
            Assert.Contains("too large", ex.InnerException.InnerException.Message);
        }

        private async Task<List<KafkaEventData<string>>> ProduceAndConsumeAsync<TOutputFunction, TTriggerFunction>(
            Expression<Func<TOutputFunction,Task>> outputFunction,
            IEnumerable<KafkaEventData<string>> events) 
        {
            var outputMethod = (outputFunction.Body as MethodCallExpression).Method;
            var eventList = events.ToList();
            foreach (var kafkaEvent in eventList)
            {
                kafkaEvent.Topic = Constants.StringTopicWithTenPartitionsName;
            }
            var eventCount = eventList.Count;
            var output = new ConcurrentBag<KafkaEventData<string>>();
            using (var host = await StartHostAsync(new[] { typeof(TOutputFunction), typeof(TTriggerFunction) },
                serviceRegistrationCallback: s =>
                {
                    s.AddSingleton(eventList);
                    s.AddSingleton(output);
                }
             ))
            {
                var jobHost = host.GetJobHost();
                await jobHost.CallAsync(outputMethod);

                await TestHelpers.Await(() =>
                {
                    var foundCount = output.Count;
                    return foundCount == eventCount;
                });
                return output.ToList();
            }
        }

        private Task<IHost> StartHostAsync(Type testType, ILoggerProvider customLoggerProvider = null) => StartHostAsync(new[] { testType }, customLoggerProvider);

        private async Task<IHost> StartHostAsync(Type[] testTypes, ILoggerProvider customLoggerProvider = null, Action<IServiceCollection> serviceRegistrationCallback = null)
        {
            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder
                    .AddAzureStorageBlobs()
                    .AddAzureStorageQueues()
                    .AddKafka(kafkaoption =>
                    {
                        kafkaoption.SessionTimeoutMs = 10000;
                    });
                })
                .ConfigureAppConfiguration(c =>
                {
                    c.AddTestSettings();
                    c.AddJsonFile("appsettings.tests.json", optional: true);
                    c.AddJsonFile("local.appsettings.tests.json", optional: true);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(new ExplicitTypeLocator(testTypes));
                    serviceRegistrationCallback?.Invoke(services);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(customLoggerProvider ?? loggerProvider);
                })
                .Build();

            await host.StartAsync();
            return host;
        }

        [Fact]
        public async Task StringValue_SingleTrigger_Retry()
        {
            const int producedMessagesCount = 1;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(SingleItem_Single_Partition_Raw_String_Without_Key_Trigger_Retry), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.Produce_AsyncCollector_String_Without_Key)),
                    endToEndTestFixture.StringTopicWithOnePartition.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

                await TestHelpers.Await(() =>
                {
                    Thread.Sleep(10000);
                    foreach (LogMessage logMsg in loggerProvider1.GetAllLogMessages()) {
                        if (logMsg.Exception != null)
                        {
                            Console.WriteLine(logMsg.Exception.InnerException.Message);
                        }
                    }
                    var foundCount = loggerProvider1.GetAllLogMessages().Count(p => p.FormattedMessage != null &&  p.FormattedMessage.Contains($"Executed '{nameof(SingleItem_Single_Partition_Raw_String_Without_Key_Trigger_Retry)}.Trigger'"));
					return foundCount == 6;
                });

                await Task.Delay(1500);
            }
        }

        // <summary>
        // This test expects a schema registry running at localhost:8081.
        // <see cref="Constants.SchemaRegistryUrl"/>
        // </summary>
        [Fact]
        public async Task SingleItem_With_Schema_Registry()
        {
            const int producedMessagesCount = 1;
            
            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(
                       new[] { typeof(SingleItem_With_Schema_Registry), typeof(KafkaOutputFunctions) },
                       loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions),
                        nameof(KafkaOutputFunctions.Produce_AsyncCollector_PageView_With_String_Key)),
                    new { topic = Constants.SchemaRegistryTopicName });

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null);
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }
        }

        [Fact]
        public async Task SingleItem_With_AvroKey()
        {
            const int producedMessagesCount = 5;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            
            var loggerProvider = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(
                       new[] { typeof(SingleItem_GenericAvroValue_With_GenericAvroKey_Trigger), typeof(KafkaOutputFunctions) },
                       loggerProvider))
            {
                var jobHost = host.GetJobHost();

                // Call the output function that produces messages with Avro keys
                await jobHost.CallAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), 
                        nameof(KafkaOutputFunctions.Produce_AsyncCollector_GenericRecordKeyValue)),
                    new { 
                        topic = Constants.MyKeyAvroRecordTopicName,
                        ids = Enumerable.Range(1, producedMessagesCount).Select(x => x),
                        content = Enumerable.Range(1, producedMessagesCount).Select(x => $"{messageMasterPrefix}-{x}")
                    });

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages()
                        .Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messageMasterPrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }
        }

        [Fact]
        public async Task SingleItem_With_AvroKey_SchemaRegistry()
        {
            const int producedMessagesCount = 5;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            
            var loggerProvider = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(
                       new[] { typeof(SingleItem_GenericAvroValue_With_GenericAvroKey_SchemaRegistryURL), typeof(KafkaOutputFunctions) },
                       loggerProvider))
            {
                var jobHost = host.GetJobHost();

                // Call the output function that produces messages with Avro keys using SchemaRegistry
                await jobHost.CallAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), 
                        nameof(KafkaOutputFunctions.Produce_AsyncCollector_GenericRecordKeyValue_With_SchemaRegistry)),
                    new { 
                        topic = Constants.MyKeyAvroRecordTopicName,
                        ids = Enumerable.Range(1, producedMessagesCount).Select(x => x),
                        content = Enumerable.Range(1, producedMessagesCount).Select(x => $"{messageMasterPrefix}-{x}")
                    });

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages()
                        .Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messageMasterPrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);

                await host.StopAsync();
            }
        }

        [Fact]
        public async Task SingleItem_With_NoKey_SchemaRegistry()
        {
            const int producedMessagesCount = 5;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            
            var loggerProvider = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(
                       new[] { typeof(SingleItem_With_Schema_Registry_No_Key), typeof(KafkaOutputFunctions) },
                       loggerProvider))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), 
                        nameof(KafkaOutputFunctions.Produce_AsyncCollector_GenericRecord_NoKey_With_SchemaRegistry)),
                    new { 
                        topic = Constants.SchemaRegistryNoKeyTopicName,
                        content = Enumerable.Range(1, producedMessagesCount).Select(x => $"{messageMasterPrefix}-{x}")
                    });

                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider.GetAllUserLogMessages()
                        .Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messageMasterPrefix));
                    return foundCount == producedMessagesCount;
                });

                await Task.Delay(1500);
                await host.StopAsync();
            }
        }
    }
}
