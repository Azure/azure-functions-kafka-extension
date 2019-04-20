// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaListenerTest
    {
        private ConsumeResult<TKey, TValue> CreateConsumeResult<TKey, TValue>(TValue value, int partition, long offset)
        {
            var msg = new Message<TKey, TValue>()
            {
                Value = value,
                Timestamp = Timestamp.Default,
            };

            var res = new ConsumeResult<TKey, TValue>();
            res.Message = msg;
            res.Topic = "topic";
            res.Partition = partition;
            res.Offset = offset;

            return res;
        }

        [Fact]
        public async Task When_Using_SingleItem_Binding_10_Events_Should_Execute_Function_Ten_Times()
        {
            const int ExpectedEventCount = 10;

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Ignore, string>>();

            var offset = 0L;
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() =>
                {
                    if (offset < ExpectedEventCount)
                    {
                        offset++;

                        return CreateConsumeResult<Ignore, string>(offset.ToString(), 0, offset);
                    }

                    return null;
                });

            var executorFinished = new SemaphoreSlim(0);
            var executorCalls = 0;
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    Interlocked.Increment(ref executorCalls);
                    executorFinished.Release();
                })
                .ReturnsAsync(new FunctionResult(true));

            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = "testBroker",
                Topic = "topic",
                ConsumerGroup = "group1",
            };

            var target = new KafkaListenerForTest<Ignore, string>(
                executor.Object,
                singleDispatch: true,
                options: new KafkaOptions(),
                listenerConfig,
                valueDeserializer: null,
                logger: NullLogger.Instance
                );

            target.SetConsumer(consumer.Object);

            await target.StartAsync(default(CancellationToken));

            Assert.True(await executorFinished.WaitAsync(TimeSpan.FromSeconds(5)));

            await target.StopAsync(default(CancellationToken));
        }

        [Fact]
        public async Task When_Using_MultiItem_Binding_10_Events_Should_Execute_Function_Once()
        {
            const int ExpectedEventCount = 10;

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Null, string>>();

            var offset = 0L;
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() =>
                {
                    if (offset < ExpectedEventCount)
                    {
                        offset++;

                        return CreateConsumeResult<Null, string>(offset.ToString(), 0, offset);
                    }

                    return null;
                });

            var executorFinished = new SemaphoreSlim(0);
            var processedItemCount = 0;
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback<TriggeredFunctionData, CancellationToken>((td, _) =>
                {
                    var triggerData = (KafkaTriggerInput<Null, string>)td.TriggerValue;
                    var alreadyProcessed = Interlocked.Add(ref processedItemCount, triggerData.Events.Length);
                    if (alreadyProcessed == ExpectedEventCount)
                    {
                        executorFinished.Release();
                    }
                })
                .ReturnsAsync(new FunctionResult(true));

            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = "testBroker",
                Topic = "topic",
                ConsumerGroup = "group1",
            };

            var target = new KafkaListenerForTest<Null, string>(
                executor.Object,
                singleDispatch: false,
                options: new KafkaOptions(),
                listenerConfig,
                valueDeserializer: null,
                logger: NullLogger.Instance
                );

            target.SetConsumer(consumer.Object);

            await target.StartAsync(default(CancellationToken));

            Assert.True(await executorFinished.WaitAsync(TimeSpan.FromSeconds(5)));

            await target.StopAsync(default(CancellationToken));

            executor.Verify(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task When_Topic_Has_Multiple_Partitions_Should_Execute_And_Commit_In_Order(bool singleDispatch)
        {
            const long Offset_A = 0;
            const long Offset_B = 1;
            const long Offset_C = 2;
            const long Offset_D = 3;
            const long Offset_E = 4;

            const long Offset_1 = 0;
            const long Offset_2 = 1;
            const long Offset_3 = 2;
            const long Offset_4 = 3;
            const long Offset_5 = 4;

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Null, string>>();

            var committed = new ConcurrentQueue<TopicPartitionOffset>();
           
            consumer.Setup(x => x.StoreOffset(It.IsNotNull<TopicPartitionOffset>()))
                .Callback<TopicPartitionOffset>((topicPartitionOffset) =>
                {
                    committed.Enqueue(topicPartitionOffset);
                });

            // Batch 1: AB12C
            // Batch 2: 34DE5
            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("A", 0, Offset_A))
                .Returns(CreateConsumeResult<Null, string>("B", 0, Offset_B))
                .Returns(CreateConsumeResult<Null, string>("1", 1, Offset_1))
                .Returns(CreateConsumeResult<Null, string>("2", 1, Offset_2))
                .Returns(CreateConsumeResult<Null, string>("C", 0, Offset_C))
                .Returns((ConsumeResult<Null, string>)null)
                .Returns(CreateConsumeResult<Null, string>("3", 1, Offset_3))
                .Returns(CreateConsumeResult<Null, string>("4", 1, Offset_4))
                .Returns(CreateConsumeResult<Null, string>("D", 0, Offset_D))
                .Returns(CreateConsumeResult<Null, string>("E", 0, Offset_E))
                .Returns(() =>
                {
                    // from now on return null
                    consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                        .Returns((ConsumeResult<Null, string>)null);

                    return CreateConsumeResult<Null, string>("5", 1, Offset_5);
                });

            var partition0 = new ConcurrentQueue<string>();
            var partition1 = new ConcurrentQueue<string>();
            var executorFinished = new SemaphoreSlim(0);

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback<TriggeredFunctionData, CancellationToken>((t, _) =>
                {
                    var triggerData = (KafkaTriggerInput<Null, string>)t.TriggerValue;

                    if (singleDispatch)
                    {
                        Assert.Single(triggerData.Events);
                    }

                    foreach (var ev in triggerData.Events)
                    {
                        switch (ev.Partition)
                        {
                            case 0:
                                partition0.Enqueue(ev.Value.ToString());
                                break;

                            case 1:
                                partition1.Enqueue(ev.Value.ToString());
                                break;

                            default:
                                Assert.True(false, "Unknown partition");
                                break;
                        }
                    }

                    if (partition0.Count == 5 && partition1.Count == 5)
                    {
                        executorFinished.Release();
                    }
                })
                .ReturnsAsync(new FunctionResult(true));

            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = "testBroker",
                Topic = "topic",
                ConsumerGroup = "group1",
            };

            var target = new KafkaListenerForTest<Null, string>(
                executor.Object,
                singleDispatch,
                new KafkaOptions(),
                listenerConfig,
                valueDeserializer: null,
                NullLogger.Instance
                );

            target.SetConsumer(consumer.Object);

            await target.StartAsync(default(CancellationToken));

            Assert.True(await executorFinished.WaitAsync(TimeSpan.FromSeconds(5)));

            // Give time for the commit to be saved
            await Task.Delay(1500);

            Assert.Equal(new[] { "A", "B", "C", "D", "E" }, partition0.ToArray());
            Assert.Equal(new[] { "1", "2", "3", "4", "5" }, partition1.ToArray());

            // Committing will be the one we read + 1
            // Batch 1: AB12C
            // Batch 2: 34DE5
            var committedArray = committed.ToArray();
            Assert.Equal(4, committedArray.Length);
            Assert.Equal(new[] { Offset_C + 1, Offset_E + 1 }, committedArray.Where(x => x.Partition == 0).Select(x => (long)x.Offset).ToArray());
            Assert.Equal(new[] { Offset_2 + 1, Offset_5 + 1 }, committedArray.Where(x => x.Partition == 1).Select(x => (long)x.Offset).ToArray());

            await target.StopAsync(default(CancellationToken));
        }

        [Fact]
        public async Task When_Options_Are_Set_Should_Be_Set_In_Consumer_Config()
        {
            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Ignore, string>>();

            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = "testBroker",
                Topic = "topic",
                ConsumerGroup = "group1",
            };

            var kafkaOptions = new KafkaOptions();
            var target = new KafkaListenerForTest<Ignore, string>(
                executor.Object,
                true,
                kafkaOptions,
                listenerConfig,
                valueDeserializer: null,
                NullLogger.Instance
                );

            target.SetConsumer(consumer.Object);

            await target.StartAsync(default);

            Assert.Equal(6, target.ConsumerConfig.Count());
            Assert.Equal("testBroker", target.ConsumerConfig.BootstrapServers);
            Assert.Equal("group1", target.ConsumerConfig.GroupId);
            Assert.Equal(kafkaOptions.AutoCommitIntervalMs, target.ConsumerConfig.AutoCommitIntervalMs);
            Assert.Equal(true, target.ConsumerConfig.EnableAutoCommit);
            Assert.Equal(false, target.ConsumerConfig.EnableAutoOffsetStore);
            Assert.Equal(AutoOffsetReset.Earliest, target.ConsumerConfig.AutoOffsetReset);

            await target.StopAsync(default);
        }

        [Fact]
        public async Task When_Options_With_Ssal_Are_Set_Should_Be_Set_In_Consumer_Config()
        {
            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Null, string>>();

            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = "testBroker",
                Topic = "topic",
                ConsumerGroup = "group1",
                SaslMechanism = SaslMechanism.Plain,
                SaslPassword = "mypassword",
                SaslUsername ="myusername",
                SecurityProtocol = SecurityProtocol.SaslSsl,
            };

            var kafkaOptions = new KafkaOptions();
            var target = new KafkaListenerForTest<Null, string>(
                executor.Object,
                true,
                kafkaOptions,
                listenerConfig,
                valueDeserializer: null,
                NullLogger.Instance
                );

            target.SetConsumer(consumer.Object);

            await target.StartAsync(default);

            Assert.Equal(10, target.ConsumerConfig.Count());
            Assert.Equal("testBroker", target.ConsumerConfig.BootstrapServers);
            Assert.Equal("group1", target.ConsumerConfig.GroupId);
            Assert.Equal(kafkaOptions.AutoCommitIntervalMs, target.ConsumerConfig.AutoCommitIntervalMs);
            Assert.Equal(true, target.ConsumerConfig.EnableAutoCommit);
            Assert.Equal(false, target.ConsumerConfig.EnableAutoOffsetStore);
            Assert.Equal(AutoOffsetReset.Earliest, target.ConsumerConfig.AutoOffsetReset);
            Assert.Equal(SaslMechanism.Plain, target.ConsumerConfig.SaslMechanism);
            Assert.Equal("mypassword", target.ConsumerConfig.SaslPassword);
            Assert.Equal("myusername", target.ConsumerConfig.SaslUsername);
            Assert.Equal(SecurityProtocol.SaslSsl, target.ConsumerConfig.SecurityProtocol);

            await target.StopAsync(default);
        }
    }
}