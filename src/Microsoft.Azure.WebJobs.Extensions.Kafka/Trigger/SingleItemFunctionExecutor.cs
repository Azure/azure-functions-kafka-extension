// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Executes the functions for an specific partition
    /// Used for functions that are expecting a single item.
    /// </summary>
    public class SingleItemFunctionExecutor<TKey, TValue> : FunctionExecutorBase<TKey, TValue>
    {
        private readonly string consumerGroup;

        public SingleItemFunctionExecutor(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, string consumerGroup, int channelCapacity, int channelFullRetryIntervalInMs, ICommitStrategy<TKey, TValue> commitStrategy, ILogger logger, IDrainModeManager drainModeManager, KafkaOptions options)
            : base(executor, consumer, channelCapacity, channelFullRetryIntervalInMs, commitStrategy, logger, drainModeManager, options)
        {
            this.consumerGroup = consumerGroup;
            logger.LogInformation($"FunctionExecutor Loaded: {nameof(SingleItemFunctionExecutor<TKey, TValue>)}");
        }

        protected override async Task ReaderAsync(ChannelReader<IKafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger)
        {
            var partitionTasks = new List<Task>();

            while (!cancellationToken.IsCancellationRequested && await reader.WaitToReadAsync(cancellationToken))
            {
                while (!cancellationToken.IsCancellationRequested && reader.TryRead(out var itemsToExecute))
                {
                    try
                    {
                        partitionTasks.Clear();

                        // Create one task per partition, this way slow partition executions will not delay others
                        // Order in a partition must be followed.
                        var itemsByPartition = itemsToExecute.GroupBy(x => x.Partition);

                        foreach (var partitionAndEvents in itemsByPartition)
                        {
                            var partition = partitionAndEvents.Key;
                            var kafkaEvents = partitionAndEvents;

                            partitionTasks.Add(ProcessPartitionItemsAsync(partition, kafkaEvents, cancellationToken));
                        }

                        await Task.WhenAll(partitionTasks);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error in executor reader");
                    }
                }
            }

            logger.LogInformation("Exiting reader {processName}", nameof(SingleItemFunctionExecutor<TKey, TValue>));
        }

        private async Task ProcessPartitionItemsAsync(int partition, IEnumerable<IKafkaEventData> events, CancellationToken cancellationToken)
        {
            TopicPartition topicPartition = null;
            foreach (var kafkaEventData in events)
            {
                if (topicPartition == null)
                {
                    topicPartition = new TopicPartition(kafkaEventData.Topic, partition);
                }

                var committed = false;
                while (!committed && !cancellationToken.IsCancellationRequested)
                {
                    var triggerInput = KafkaTriggerInput.New(kafkaEventData);
                    var triggerData = new TriggeredFunctionData
                    {
                        TriggerValue = triggerInput,
                    };

                    // Create Single Event Activity Provider and Start the activity
                    var singleEventActivityProvider = new SingleEventActivityProvider(kafkaEventData, consumerGroup);
                    singleEventActivityProvider.StartActivity();
                    FunctionResult functionResult = null;
                    try
                    {
                        // Execute the Function
                        functionResult = await this.ExecuteFunctionAsync(triggerData, cancellationToken);
                        // Set the status of activity.
                        singleEventActivityProvider.SetActivityStatus(functionResult.Succeeded, functionResult.Exception);
                    }
                    catch (Exception ex)
                    {
                        singleEventActivityProvider.SetActivityStatus(false, ex);
                        throw;
                    }
                    finally
                    {
                        // Stop the activity
                        singleEventActivityProvider.StopCurrentActivity();
                    }

                    if (functionResult.Succeeded)
                    {
                        this.ClearRetryCounter(kafkaEventData.Topic, partition, kafkaEventData.Offset);
                        this.Commit(new[] { new TopicPartitionOffset(topicPartition, kafkaEventData.Offset + 1) });
                        committed = true;
                    }
                    else if (this.options.CommitOnFailure)
                    {
                        // Default at-most-once behavior: commit regardless of failure
                        this.Commit(new[] { new TopicPartitionOffset(topicPartition, kafkaEventData.Offset + 1) });
                        committed = true;
                    }
                    else if (this.IncrementRetryAndCheckExceeded(kafkaEventData.Topic, partition, kafkaEventData.Offset))
                    {
                        // Poison message — max retries exceeded, force-commit to skip
                        logger.LogError(functionResult.Exception,
                            "Message at {topic} / {partition} / {offset} failed after {maxRetries} retries. " +
                            "Offset will be force-committed and the message will be skipped. " +
                            "Consider implementing dead-letter handling in your function code.",
                            kafkaEventData.Topic, partition, kafkaEventData.Offset, this.options.MaxRetries);
                        this.ClearRetryCounter(kafkaEventData.Topic, partition, kafkaEventData.Offset);
                        this.Commit(new[] { new TopicPartitionOffset(topicPartition, kafkaEventData.Offset + 1) });
                        committed = true;
                    }
                    else
                    {
                        // At-least-once: retry the same message in-place
                        logger.LogWarning(functionResult.Exception,
                            "Function execution failed for {topic} / {partition} / {offset}. " +
                            "Message will be retried in-place.",
                            kafkaEventData.Topic, partition, kafkaEventData.Offset);
                    }
                }
            }
        }
    }
}