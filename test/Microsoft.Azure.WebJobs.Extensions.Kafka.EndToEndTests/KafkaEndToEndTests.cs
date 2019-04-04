// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Tests;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
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
            this.loggerProvider = CreateTestLoggerProvider();
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

            using (var host = await StartHostAsync(new[] { typeof(SingleItemTrigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithOnePartition.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(SingleItemTrigger), typeof(KafkaOutputFunctions) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithOnePartition.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(MultiItemTrigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithOnePartition.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItemTrigger) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithOnePartition.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(SingleItemTriggerTenPartitions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithTenPartitions.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(SingleItemTriggerTenPartitions) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithTenPartitions.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItemTriggerTenPartitions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithTenPartitions.Name,
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

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MultiItemTriggerTenPartitions) }, loggerProvider2))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                    this.endToEndTestFixture.StringTopicWithTenPartitions.Name,
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

            var producerHost = await this.StartHostAsync(typeof(KafkaOutputFunctions));
            var producerJobHost = producerHost.GetJobHost();

            var host2HasPartitionsSemaphore = new SemaphoreSlim(0);

            // Split the call in 4, waiting 1sec between calls
            var producerTask = Task.Run(async () => 
            {
                var allMessages = Enumerable.Range(1, producedMessagesCount).Select(x => EndToEndTestExtensions.CreateMessageValue(messagePrefix, x));
                const int loopCount = 4;
                var itemsPerLoop = producedMessagesCount / loopCount;
                for (var i=0; i < loopCount; ++i)
                {
                    var messages = allMessages.Skip(i * itemsPerLoop).Take(itemsPerLoop);
                    await producerJobHost.CallOutputTriggerStringAsync(
                        GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringTopic)),
                        this.endToEndTestFixture.StringTopicWithTenPartitions.Name,
                        messages);

                    if (i == 0)
                    {
                        // wait until host2 has partitions assigned
                        Assert.True(await host2HasPartitionsSemaphore.WaitAsync(TimeSpan.FromSeconds(30)), "Host2 has not been assigned any partition after waiting for 30 seconds");
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

                host1 = await StartHostAsync(typeof(MultiItemTriggerTenPartitions), host1Log);

                // wait until host1 receives partitions
                await TestHelpers.Await(() =>
                {
                    var host1HasPartitions = host1Log.GetAllLogMessages().Any(x => x.FormattedMessage != null && x.FormattedMessage.Contains("Assigned partitions"));
                    return host1HasPartitions;
                });


                host2 = await StartHostAsync(typeof(MultiItemTriggerTenPartitions), host2Log);

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

        [Fact]
        public async Task Produce_And_Consume_With_Key_OfType_Long()
        {
            const int producedMessagesCount = 80;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(new[] { typeof(StringTopicWithLongKeyAndTenPartitionsTrigger), typeof(KafkaOutputFunctions) }, loggerProvider1))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringWithLongKeyAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendToStringWithLongKeyTopic)),
                    this.endToEndTestFixture.StringTopicWithLongKeyAndTenPartitions.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefix + x),
                    Enumerable.Range(1, producedMessagesCount).Select(x => x % 20L));
                    
                await TestHelpers.Await(() =>
                {
                    var foundCount = loggerProvider1.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);
            }
        }

        [Fact]
        public async Task Produce_And_Consume_Specific_Avro()
        {
            const int producedMessagesCount = 80;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MyRecordAvroTrigger) }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringWithStringKeyAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendAvroWithStringKeyTopic)),
                    this.endToEndTestFixture.MyAvroRecordTopic.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefix + x),
                    Enumerable.Range(1, producedMessagesCount).Select(x => "record_" + (x % 20).ToString())
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = this.loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);
            }
        }

        [Fact]
        public async Task Produce_And_Consume_Protobuf()
        {
            const int producedMessagesCount = 80;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            using (var host = await StartHostAsync(new[] { typeof(KafkaOutputFunctions), typeof(MyProtobufTrigger) }))
            {
                var jobHost = host.GetJobHost();

                await jobHost.CallOutputTriggerStringWithStringKeyAsync(
                    GetStaticMethod(typeof(KafkaOutputFunctions), nameof(KafkaOutputFunctions.SendProtobufWithStringKeyTopic)),
                    this.endToEndTestFixture.MyProtobufTopic.Name,
                    Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefix + x),
                    Enumerable.Range(1, producedMessagesCount).Select(x => "record_" + (x % 20).ToString())
                    );

                await TestHelpers.Await(() =>
                {
                    var foundCount = this.loggerProvider.GetAllUserLogMessages().Count(p => p.FormattedMessage != null && p.FormattedMessage.Contains(messagePrefix));
                    return foundCount == producedMessagesCount;
                });

                // Give time for the commit to be saved
                await Task.Delay(1500);
            }
        }


        private Task<IHost> StartHostAsync(Type testType, ILoggerProvider customLoggerProvider = null) => StartHostAsync(new[] { testType }, customLoggerProvider);

        private async Task<IHost> StartHostAsync(Type[] testTypes, ILoggerProvider customLoggerProvider = null)
        {
            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder
                    .AddAzureStorage()
                    .AddKafka();
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
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddProvider(customLoggerProvider ?? this.loggerProvider);
                })
                .Build();

            await host.StartAsync();
            return host;
        }
    }
}
