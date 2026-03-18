// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    /// <summary>
    /// Tests for at-least-once delivery behavior (Issue #614).
    /// Validates that offset commits are skipped on function failure,
    /// CommitOnFailure opt-in, and maxRetries poison message protection.
    /// </summary>
    public class AtLeastOnceDeliveryTest
    {
        private ConsumeResult<TKey, TValue> CreateConsumeResult<TKey, TValue>(TValue value, int partition, long offset, string topic = "topic")
        {
            var msg = new Message<TKey, TValue>()
            {
                Value = value,
                Timestamp = Timestamp.Default,
            };

            var res = new ConsumeResult<TKey, TValue>();
            res.Message = msg;
            res.Topic = topic;
            res.Partition = partition;
            res.Offset = offset;

            return res;
        }

        private (Mock<ITriggeredFunctionExecutor> executor, Mock<IConsumer<Null, string>> consumer, ConcurrentQueue<TopicPartitionOffset> committed) CreateMocks()
        {
            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Null, string>>();
            var committed = new ConcurrentQueue<TopicPartitionOffset>();

            consumer.Setup(x => x.StoreOffset(It.IsNotNull<TopicPartitionOffset>()))
                .Callback<TopicPartitionOffset>((tpo) => committed.Enqueue(tpo));

            return (executor, consumer, committed);
        }

        private KafkaListenerForTest<Null, string> CreateListener(
            Mock<ITriggeredFunctionExecutor> executor,
            Mock<IConsumer<Null, string>> consumer,
            bool singleDispatch,
            KafkaOptions options = null)
        {
            options = options ?? new KafkaOptions();
            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = "testBroker",
                Topic = "topic",
                ConsumerGroup = "group1",
            };

            var target = new KafkaListenerForTest<Null, string>(
                executor.Object,
                singleDispatch,
                options,
                listenerConfig,
                requiresKey: true,
                valueDeserializer: null,
                keyDeserializer: null,
                NullLogger.Instance,
                functionId: "testId",
                drainModeManager: null);

            target.SetConsumer(consumer.Object);
            return target;
        }

        // ====================================================================
        // Single-dispatch: Function fails → offset NOT committed
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_OffsetNotCommitted()
        {
            var (executor, consumer, committed) = CreateMocks();

            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns((ConsumeResult<Null, string>)null);

            var executorCalled = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => executorCalled.Release())
                .ReturnsAsync(new FunctionResult(false));

            var target = CreateListener(executor, consumer, singleDispatch: true);

            await target.StartAsync(default);
            Assert.True(await executorCalled.WaitAsync(TimeSpan.FromSeconds(5)));

            // Give time for commit to _not_ happen
            await Task.Delay(500);

            await target.StopAsync(default);

            // Offset should NOT have been committed
            Assert.Empty(committed);
        }

        // ====================================================================
        // Single-dispatch: Function fails → stops processing remaining events
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_StopsPartitionProcessing()
        {
            var (executor, consumer, committed) = CreateMocks();

            // 3 events on same partition
            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns(CreateConsumeResult<Null, string>("msg2", 0, 1))
                .Returns(CreateConsumeResult<Null, string>("msg3", 0, 2))
                .Returns((ConsumeResult<Null, string>)null);

            var executorCalls = 0;
            var executorDone = new SemaphoreSlim(0);

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Returns<TriggeredFunctionData, CancellationToken>((td, _) =>
                {
                    var call = Interlocked.Increment(ref executorCalls);
                    if (call >= 2)
                    {
                        executorDone.Release();
                    }

                    // First message succeeds, second fails
                    if (call == 1)
                    {
                        return Task.FromResult(new FunctionResult(true));
                    }
                    else
                    {
                        return Task.FromResult(new FunctionResult(false));
                    }
                });

            var target = CreateListener(executor, consumer, singleDispatch: true);

            await target.StartAsync(default);
            Assert.True(await executorDone.WaitAsync(TimeSpan.FromSeconds(5)));

            await Task.Delay(500);
            await target.StopAsync(default);

            // Only the first message's offset should be committed
            Assert.Single(committed);
            Assert.Equal(1, committed.First().Offset);  // offset 0 + 1

            // 3rd message should NOT have been executed (break on failure)
            Assert.Equal(2, executorCalls);
        }

        // ====================================================================
        // Single-dispatch: CommitOnFailure=true → offset committed on failure
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_CommitOnFailureTrue_OffsetCommitted()
        {
            var (executor, consumer, committed) = CreateMocks();

            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns((ConsumeResult<Null, string>)null);

            var executorCalled = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => executorCalled.Release())
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = true };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);
            Assert.True(await executorCalled.WaitAsync(TimeSpan.FromSeconds(5)));

            await Task.Delay(500);
            await target.StopAsync(default);

            // Offset SHOULD be committed (legacy behavior)
            Assert.Single(committed);
            Assert.Equal(1, committed.First().Offset);
        }

        // ====================================================================
        // Single-dispatch: MaxRetries exceeded → force-commit
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_MaxRetriesExceeded_ForceCommits()
        {
            var (executor, consumer, committed) = CreateMocks();
            var maxRetries = 2;

            // Return the same message repeatedly (simulating redelivery)
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() => CreateConsumeResult<Null, string>("poison", 0, 5));

            var executorCallCount = 0;
            var forceCommitted = new SemaphoreSlim(0);

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Returns<TriggeredFunctionData, CancellationToken>((td, _) =>
                {
                    Interlocked.Increment(ref executorCallCount);
                    return Task.FromResult(new FunctionResult(false));
                });

            var options = new KafkaOptions { MaxRetries = maxRetries };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Wait for enough retries + force-commit
            // MaxRetries=2 means: 1st try + 2 retries = 3 executions, on 4th the counter exceeds
            // Actually: HasExceededMaxRetries increments and checks > maxRetries
            // Call 1: count=1, >2? no. Call 2: count=2, >2? no. Call 3: count=3, >2? yes → force-commit
            await Task.Delay(3000);

            await target.StopAsync(default);

            // Should have force-committed after maxRetries+1 attempts
            Assert.True(committed.Count > 0, "Should have force-committed the poison message");
            Assert.Equal(6, committed.First().Offset);  // offset 5 + 1
        }

        // ====================================================================
        // Single-dispatch: MaxRetries=-1 (unlimited) → never force-commits
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_UnlimitedRetries_NeverForceCommits()
        {
            var (executor, consumer, committed) = CreateMocks();

            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() => CreateConsumeResult<Null, string>("fail", 0, 0));

            var executorCallCount = 0;
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => Interlocked.Increment(ref executorCallCount))
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { MaxRetries = -1 };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Let it retry several times
            await Task.Delay(2000);

            await target.StopAsync(default);

            // Should never have committed
            Assert.Empty(committed);
            // But should have been called multiple times
            Assert.True(executorCallCount > 3, $"Expected more than 3 retries, got {executorCallCount}");
        }

        // ====================================================================
        // Single-dispatch: Function succeeds → offset committed (regression)
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionSucceeds_OffsetCommitted()
        {
            var (executor, consumer, committed) = CreateMocks();

            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns((ConsumeResult<Null, string>)null);

            var executorCalled = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => executorCalled.Release())
                .ReturnsAsync(new FunctionResult(true));

            var target = CreateListener(executor, consumer, singleDispatch: true);

            await target.StartAsync(default);
            Assert.True(await executorCalled.WaitAsync(TimeSpan.FromSeconds(5)));

            await Task.Delay(500);
            await target.StopAsync(default);

            // Offset should be committed
            Assert.Single(committed);
            Assert.Equal(1, committed.First().Offset);
        }

        // ====================================================================
        // Batch-dispatch: Function fails → offset NOT committed
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_OffsetNotCommitted()
        {
            var (executor, consumer, committed) = CreateMocks();

            var offset = 0L;
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() =>
                {
                    if (offset < 5)
                    {
                        offset++;
                        return CreateConsumeResult<Null, string>(offset.ToString(), 0, offset);
                    }
                    return null;
                });

            var executorCalled = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => executorCalled.Release())
                .ReturnsAsync(new FunctionResult(false));

            var target = CreateListener(executor, consumer, singleDispatch: false);

            await target.StartAsync(default);
            Assert.True(await executorCalled.WaitAsync(TimeSpan.FromSeconds(5)));

            await Task.Delay(500);
            await target.StopAsync(default);

            // Offset should NOT have been committed
            Assert.Empty(committed);
        }

        // ====================================================================
        // Batch-dispatch: CommitOnFailure=true → offset committed on failure
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_CommitOnFailureTrue_OffsetCommitted()
        {
            var (executor, consumer, committed) = CreateMocks();

            var offset = 0L;
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() =>
                {
                    if (offset < 3)
                    {
                        offset++;
                        return CreateConsumeResult<Null, string>(offset.ToString(), 0, offset);
                    }
                    return null;
                });

            var executorCalled = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => executorCalled.Release())
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = true };
            var target = CreateListener(executor, consumer, singleDispatch: false, options: options);

            await target.StartAsync(default);
            Assert.True(await executorCalled.WaitAsync(TimeSpan.FromSeconds(5)));

            await Task.Delay(500);
            await target.StopAsync(default);

            // Offset SHOULD be committed (legacy behavior)
            Assert.NotEmpty(committed);
        }

        // ====================================================================
        // Batch-dispatch: MaxRetries exceeded → force-commit 
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_MaxRetriesExceeded_ForceCommits()
        {
            var (executor, consumer, committed) = CreateMocks();
            var maxRetries = 2;

            // Always return the same batch
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() => CreateConsumeResult<Null, string>("batch-poison", 0, 10));

            var executorCallCount = 0;
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => Interlocked.Increment(ref executorCallCount))
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { MaxRetries = maxRetries };
            var target = CreateListener(executor, consumer, singleDispatch: false, options: options);

            await target.StartAsync(default);

            // Wait for retries + force-commit
            await Task.Delay(3000);

            await target.StopAsync(default);

            // Should have force-committed
            Assert.True(committed.Count > 0, "Should have force-committed the batch after max retries");
        }

        // ====================================================================
        // Batch-dispatch: Function succeeds → offset committed (regression)
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionSucceeds_OffsetCommitted()
        {
            var (executor, consumer, committed) = CreateMocks();

            var offset = 0L;
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() =>
                {
                    if (offset < 3)
                    {
                        offset++;
                        return CreateConsumeResult<Null, string>(offset.ToString(), 0, offset);
                    }
                    return null;
                });

            var executorCalled = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() => executorCalled.Release())
                .ReturnsAsync(new FunctionResult(true));

            var target = CreateListener(executor, consumer, singleDispatch: false);

            await target.StartAsync(default);
            Assert.True(await executorCalled.WaitAsync(TimeSpan.FromSeconds(5)));

            await Task.Delay(500);
            await target.StopAsync(default);

            // Offset should be committed
            Assert.NotEmpty(committed);
        }

        // ====================================================================
        // KafkaOptions: MaxRetries validation
        // ====================================================================
        [Fact]
        public void KafkaOptions_MaxRetries_RejectsInvalidValues()
        {
            var options = new KafkaOptions();

            // -1 is valid (unlimited)
            options.MaxRetries = -1;
            Assert.Equal(-1, options.MaxRetries);

            // 0 is valid (no retries)
            options.MaxRetries = 0;
            Assert.Equal(0, options.MaxRetries);

            // Positive values are valid
            options.MaxRetries = 10;
            Assert.Equal(10, options.MaxRetries);

            // -2 is invalid
            Assert.Throws<InvalidOperationException>(() => options.MaxRetries = -2);
        }

        // ====================================================================
        // KafkaOptions: Default values
        // ====================================================================
        [Fact]
        public void KafkaOptions_Defaults_AreCorrect()
        {
            var options = new KafkaOptions();

            Assert.False(options.CommitOnFailure);
            Assert.Equal(5, options.MaxRetries);
        }
    }
}
