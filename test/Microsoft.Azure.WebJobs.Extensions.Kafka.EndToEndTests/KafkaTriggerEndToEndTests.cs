// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    [Trait("Category", "E2E")]
    public class KafkaTriggerEndToEndTests
    {
        const string Broker = "localhost:9092";
        const string StringTopicWithOnePartition = "stringTopicOnePartition";
        const string StringTopicWithTenPartitions = "stringTopicTenPartitions";
        private readonly TestLoggerProvider loggerProvider;

        internal static TestLoggerProvider CreateTestLoggerProvider()
        {
            return (System.Diagnostics.Debugger.IsAttached) ?
                new TestLoggerProvider((l) => System.Diagnostics.Debug.WriteLine(l.ToString())) :
                new TestLoggerProvider();
        }

        public KafkaTriggerEndToEndTests()
        {
            this.loggerProvider = CreateTestLoggerProvider();
        }

        [Fact]
        public async Task StringValue_SingleTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(typeof(SingleItemTrigger), loggerProvider1))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithOnePartition, Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

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

            using (var host = await StartHostAsync(typeof(SingleItemTrigger), loggerProvider2))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithOnePartition, Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

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
        public async Task SinglePartition_StringValue_ArrayTrigger_Resume_Continue_Where_Stopped()
        {
            const int producedMessagesCount = 80;
            var messageMasterPrefix = Guid.NewGuid().ToString();
            var messagePrefixBatch1 = messageMasterPrefix + ":1:";
            var messagePrefixBatch2 = messageMasterPrefix + ":2:";

            var loggerProvider1 = CreateTestLoggerProvider();

            using (var host = await StartHostAsync(typeof(MultiItemTrigger), loggerProvider1))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithOnePartition, Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

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

            using (var host = await StartHostAsync(typeof(MultiItemTrigger), loggerProvider2))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithOnePartition, Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

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

            using (var host = await StartHostAsync(typeof(SingleItemTriggerTenPartitions), loggerProvider1))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithTenPartitions, Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

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

            using (var host = await StartHostAsync(typeof(SingleItemTriggerTenPartitions), loggerProvider2))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithTenPartitions, Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

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

            using (var host = await StartHostAsync(typeof(MultiItemTriggerTenPartitions), loggerProvider1))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithTenPartitions, Enumerable.Range(1, producedMessagesCount).Select(x => messagePrefixBatch1 + x));

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

            using (var host = await StartHostAsync(typeof(MultiItemTriggerTenPartitions), loggerProvider2))
            {
                await KafkaProducers.ProduceStringsAsync(Broker, StringTopicWithTenPartitions, Enumerable.Range(1 + producedMessagesCount, producedMessagesCount).Select(x => messagePrefixBatch2 + x));

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
        /// </summary>
        [Fact]
        public async Task Multiple_Hosts_Process_Events_At_Least_Once()
        {
            const int producedMessagesCount = 240;
            var messagePrefix = Guid.NewGuid().ToString() + ":";

            var producerTask = KafkaProducers.ProduceStringsAsync(
                Broker, 
                StringTopicWithTenPartitions, 
                Enumerable.Range(1, producedMessagesCount).Select(x => KafkaProducers.CreateMessageValue(messagePrefix, x)), 
                TimeSpan.FromMilliseconds(100));

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
                    return host2HasPartitions;
                });

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
                        var currentMessage = KafkaProducers.CreateMessageValue(messagePrefix, i);
                        var count = allLogs.Count(x => x == currentMessage);
                        if (count == 0)
                        {
                            return false;
                        }
                    }

                    return true;
                });

                // For history write down items that have more than once
                var logs = new List<string>(host1Log.GetAllLogMessages().Where(messageFilter).Select(x => x.FormattedMessage));
                logs.AddRange(host2Log.GetAllLogMessages().Where(messageFilter).Select(x => x.FormattedMessage));

                for (int i = 1; i <= producedMessagesCount; i++)
                {
                    var currentMessage = KafkaProducers.CreateMessageValue(messagePrefix, i);
                    var count = logs.Count(x => x == currentMessage);
                    if (count > 1)
                    {
                        Console.WriteLine($"{currentMessage} was processed {count} times");
                    }
                }

            }
            finally
            {
                await host1?.StopAsync();
                await host2?.StopAsync();
            }

            await producerTask;
        }



        private async Task<IHost> StartHostAsync(Type testType, ILoggerProvider customLoggerProvider = null)
        {
            ExplicitTypeLocator locator = new ExplicitTypeLocator(testType);

            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder
                    .AddAzureStorage()
                    .AddKafka();
                })
                .ConfigureAppConfiguration(c =>
                {
                    //c.AddTestSettings();
                    //c.AddJsonFile("appsettings.tests.json", optional: false);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(locator);
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

        private static class SingleItemTrigger
        {
            public static void Trigger(
                [KafkaTrigger(Broker, StringTopicWithOnePartition, ConsumerGroup = "EndToEndTestClass-Trigger")] KafkaEventData kafkaEvent,
                ILogger log)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }

        private static class MultiItemTrigger
        {
            public static void Trigger(
                [KafkaTrigger(Broker, StringTopicWithOnePartition, ConsumerGroup = "EndToEndTestClass-Trigger")] KafkaEventData[] kafkaEvents,
                ILogger log)
            {
                foreach (var kafkaEvent in kafkaEvents)
                {
                    log.LogInformation(kafkaEvent.Value.ToString());
                }
            }
        }

        private static class SingleItemTriggerTenPartitions
        {
            public static void Trigger(
                [KafkaTrigger(Broker, StringTopicWithTenPartitions, ConsumerGroup = "EndToEndTestClass-Trigger")] KafkaEventData kafkaEvent,
                ILogger log)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }

        private static class MultiItemTriggerTenPartitions
        {
            public static void Trigger(
                [KafkaTrigger(Broker, StringTopicWithTenPartitions, ConsumerGroup = "EndToEndTestClass-Trigger")] KafkaEventData[] kafkaEvents,
                ILogger log)
            {
                foreach (var kafkaEvent in kafkaEvents)
                {
                    log.LogInformation(kafkaEvent.Value.ToString());
                }
            }
        }
    }
}
