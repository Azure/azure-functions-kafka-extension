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
        /// <summary>
        /// Default timeout for all async waits. Long enough for slow CI, short enough to fail fast.
        /// </summary>
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

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

        /// <summary>
        /// Creates mock executor, consumer, and a queue that captures committed offsets.
        /// The commitSignal is released each time StoreOffset is called.
        /// </summary>
        private (Mock<ITriggeredFunctionExecutor> executor, Mock<IConsumer<Null, string>> consumer, ConcurrentQueue<TopicPartitionOffset> committed, SemaphoreSlim commitSignal) CreateMocks()
        {
            var executor = new Mock<ITriggeredFunctionExecutor>();
            var consumer = new Mock<IConsumer<Null, string>>();
            var committed = new ConcurrentQueue<TopicPartitionOffset>();
            var commitSignal = new SemaphoreSlim(0);

            consumer.Setup(x => x.StoreOffset(It.IsNotNull<TopicPartitionOffset>()))
                .Callback<TopicPartitionOffset>((tpo) =>
                {
                    committed.Enqueue(tpo);
                    commitSignal.Release();
                });

            return (executor, consumer, committed, commitSignal);
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
        // Single-dispatch: Function fails → offset NOT committed (CommitOnFailure=false)
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_OffsetNotCommitted()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns((ConsumeResult<Null, string>)null);

            // Function fails, then succeeds (in-place retry)
            var callCount = 0;
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Returns<TriggeredFunctionData, CancellationToken>((td, _) =>
                {
                    var c = Interlocked.Increment(ref callCount);
                    if (c == 1)
                    {
                        return Task.FromResult(new FunctionResult(false));
                    }

                    return Task.FromResult(new FunctionResult(true));
                });

            var options = new KafkaOptions { CommitOnFailure = false };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Wait for commit (retry succeeds on 2nd attempt)
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Should commit after successful retry");

            await target.StopAsync(default);

            // Should have been called at least 2 times (1 fail + 1 success)
            Assert.True(callCount >= 2, $"Expected at least 2 calls, got {callCount}");
            Assert.Single(committed);
            Assert.Equal(1, committed.First().Offset);
        }

        // ====================================================================
        // Single-dispatch: Function fails → retries in-place then succeeds
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_RetriesInPlaceThenSucceeds()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

            // 3 events on same partition
            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns(CreateConsumeResult<Null, string>("msg2", 0, 1))
                .Returns(CreateConsumeResult<Null, string>("msg3", 0, 2))
                .Returns((ConsumeResult<Null, string>)null);

            var executorCalls = 0;

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Returns<TriggeredFunctionData, CancellationToken>((td, _) =>
                {
                    var call = Interlocked.Increment(ref executorCalls);
                    // msg1: call 1 fails, call 2 succeeds
                    // msg2: call 3 succeeds
                    // msg3: call 4 succeeds
                    if (call == 1)
                    {
                        return Task.FromResult(new FunctionResult(false));
                    }

                    return Task.FromResult(new FunctionResult(true));
                });

            var options = new KafkaOptions { CommitOnFailure = false };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Wait for all 3 messages to be committed (msg1 retried, msg2 and msg3 succeed directly)
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "First commit");
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Second commit");
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Third commit");

            await target.StopAsync(default);

            // All 3 messages committed, msg1 took 2 tries
            Assert.Equal(3, committed.Count);
            Assert.Equal(4, executorCalls);  // 1 fail + 3 success
        }

        // ====================================================================
        // Single-dispatch: CommitOnFailure=true → offset committed on failure
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_CommitOnFailureTrue_OffsetCommitted()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns((ConsumeResult<Null, string>)null);

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = true };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Wait for the commit (should happen despite failure)
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Offset should be committed with CommitOnFailure=true");

            await target.StopAsync(default);

            Assert.Single(committed);
            Assert.Equal(1, committed.First().Offset);
        }

        // ====================================================================
        // Single-dispatch: MaxRetries exceeded → force-commit (CommitOnFailure=false)
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_MaxRetriesExceeded_ForceCommits()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();
            var maxRetries = 2;

            // Return the same message repeatedly (simulating redelivery)
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() => CreateConsumeResult<Null, string>("poison", 0, 5));

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = false, MaxRetries = maxRetries };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Wait for the force-commit (after maxRetries+1 attempts via in-place retry)
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Should have force-committed the poison message");

            await target.StopAsync(default);

            Assert.True(committed.Count > 0);
            Assert.Equal(6, committed.First().Offset);  // offset 5 + 1
        }

        // ====================================================================
        // Single-dispatch: MaxRetries=-1 (unlimited) → keeps retrying (CommitOnFailure=false)
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionFails_UnlimitedRetries_NeverForceCommits()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() => CreateConsumeResult<Null, string>("fail", 0, 0));

            var executorCallCount = 0;
            var enoughRetries = new SemaphoreSlim(0);
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    if (Interlocked.Increment(ref executorCallCount) >= 10)
                    {
                        enoughRetries.Release();
                    }
                })
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = false, MaxRetries = -1 };
            var target = CreateListener(executor, consumer, singleDispatch: true, options: options);

            await target.StartAsync(default);

            // Wait for at least 10 retries
            Assert.True(await enoughRetries.WaitAsync(TestTimeout), "Should have retried at least 10 times");

            await target.StopAsync(default);

            // Should never have committed despite many retries
            Assert.Empty(committed);
            Assert.True(executorCallCount >= 10);
        }

        // ====================================================================
        // Single-dispatch: Function succeeds → offset committed (regression)
        // ====================================================================
        [Fact]
        public async Task SingleItem_FunctionSucceeds_OffsetCommitted()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

            consumer.SetupSequence(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(CreateConsumeResult<Null, string>("msg1", 0, 0))
                .Returns((ConsumeResult<Null, string>)null);

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(true));

            var target = CreateListener(executor, consumer, singleDispatch: true);

            await target.StartAsync(default);

            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Offset should be committed on success");

            await target.StopAsync(default);

            Assert.Single(committed);
            Assert.Equal(1, committed.First().Offset);
        }

        // ====================================================================
        // Batch-dispatch: Function fails → offset NOT committed (CommitOnFailure=false, retries until max)
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_OffsetNotCommitted()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

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

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(false));

            // CommitOnFailure=false with maxRetries: eventually force-commits
            var options = new KafkaOptions { CommitOnFailure = false, MaxRetries = 2 };
            var target = CreateListener(executor, consumer, singleDispatch: false, options: options);

            await target.StartAsync(default);

            // Should eventually force-commit after max retries
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Should force-commit after max retries");

            await target.StopAsync(default);
            Assert.NotEmpty(committed);
        }

        // ====================================================================
        // Batch-dispatch: CommitOnFailure=true → offset committed on failure
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_CommitOnFailureTrue_OffsetCommitted()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

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

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = true };
            var target = CreateListener(executor, consumer, singleDispatch: false, options: options);

            await target.StartAsync(default);

            // Wait for the commit (should happen despite failure)
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Offset should be committed with CommitOnFailure=true");

            await target.StopAsync(default);
            Assert.NotEmpty(committed);
        }

        // ====================================================================
        // Batch-dispatch: MaxRetries exceeded → force-commit (CommitOnFailure=false)
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_MaxRetriesExceeded_ForceCommits()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();
            var maxRetries = 2;

            // Always return the same batch
            consumer.Setup(x => x.Consume(It.IsNotNull<TimeSpan>()))
                .Returns(() => CreateConsumeResult<Null, string>("batch-poison", 0, 10));

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(false));

            var options = new KafkaOptions { CommitOnFailure = false, MaxRetries = maxRetries };
            var target = CreateListener(executor, consumer, singleDispatch: false, options: options);

            await target.StartAsync(default);

            // Wait for the force-commit
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Should have force-committed the batch after max retries");

            await target.StopAsync(default);
            Assert.True(committed.Count > 0);
        }

        // ====================================================================
        // Batch-dispatch: Function fails then succeeds on retry (CommitOnFailure=false)
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionFails_RetriesInPlaceThenSucceeds()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

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

            var callCount = 0;
            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .Returns<TriggeredFunctionData, CancellationToken>((td, _) =>
                {
                    var c = Interlocked.Increment(ref callCount);
                    // First execution fails, second (retry) succeeds
                    if (c == 1)
                    {
                        return Task.FromResult(new FunctionResult(false));
                    }

                    return Task.FromResult(new FunctionResult(true));
                });

            var options = new KafkaOptions { CommitOnFailure = false };
            var target = CreateListener(executor, consumer, singleDispatch: false, options: options);

            await target.StartAsync(default);

            // Wait for commit (retry succeeds on 2nd attempt)
            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Should commit after successful batch retry");

            await target.StopAsync(default);

            Assert.NotEmpty(committed);
            // Should have been called at least 2 times (1 fail + 1 success)
            Assert.True(callCount >= 2, $"Expected at least 2 calls, got {callCount}");
        }

        // ====================================================================
        // Batch-dispatch: Function succeeds → offset committed (regression)
        // ====================================================================
        [Fact]
        public async Task MultiItem_FunctionSucceeds_OffsetCommitted()
        {
            var (executor, consumer, committed, commitSignal) = CreateMocks();

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

            executor.Setup(x => x.TryExecuteAsync(It.IsNotNull<TriggeredFunctionData>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FunctionResult(true));

            var target = CreateListener(executor, consumer, singleDispatch: false);

            await target.StartAsync(default);

            Assert.True(await commitSignal.WaitAsync(TestTimeout), "Offset should be committed on success");

            await target.StopAsync(default);
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

            Assert.True(options.CommitOnFailure);
            Assert.Equal(5, options.MaxRetries);
        }
    }
}
