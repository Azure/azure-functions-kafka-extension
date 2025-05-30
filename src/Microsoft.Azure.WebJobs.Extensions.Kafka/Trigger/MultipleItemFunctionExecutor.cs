﻿// Copyright (c) .NET Foundation. All rights reserved.
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
    /// Executes the functions for a specific partition.
    /// Used for functions that are expecting multiple items at once.
    /// </summary>
    public class MultipleItemFunctionExecutor<TKey, TValue> : FunctionExecutorBase<TKey, TValue>
    {
        private readonly string consumerGroup;

        public MultipleItemFunctionExecutor(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer,  string consumerGroup, int channelCapacity, int channelFullRetryIntervalInMs, ICommitStrategy<TKey, TValue> commitStrategy, ILogger logger, IDrainModeManager drainModeManager) 
            : base(executor, consumer, channelCapacity, channelFullRetryIntervalInMs, commitStrategy, logger, drainModeManager)
        {
            this.consumerGroup = consumerGroup;
            logger.LogInformation($"FunctionExecutor Loaded: {nameof(MultipleItemFunctionExecutor<TKey, TValue>)}");
        }

        protected override async Task ReaderAsync(ChannelReader<IKafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger)
        {
            while (!cancellationToken.IsCancellationRequested && await reader.WaitToReadAsync(cancellationToken))
            {
                while (!cancellationToken.IsCancellationRequested && reader.TryRead(out var itemsToExecute))
                {
                    try
                    {
                        // Try to publish them
                        var triggerInput = KafkaTriggerInput.New(itemsToExecute);
                        var triggerData = new TriggeredFunctionData
                        {
                            TriggerValue = triggerInput,
                        };

                        // Create Batch Event Activity Provider and Start the activity 
                        var batchEventActivityProvider = new BatchEventActivityProvider(itemsToExecute, consumerGroup);
                        batchEventActivityProvider.StartActivity();

                        FunctionResult functionResult = null;
                        try
                        {
                            // Execute the function
                            functionResult = await this.ExecuteFunctionAsync(triggerData, cancellationToken);
                            // Set the status of activity.
                            batchEventActivityProvider.SetActivityStatus(functionResult.Succeeded, functionResult.Exception);
                        }
                        catch (Exception ex)
                        {
                            batchEventActivityProvider.SetActivityStatus(false, ex);
                            throw;
                        }
                        finally
                        {
                            // Stop the Activity
                            batchEventActivityProvider.StopCurrentActivity();
                        }

                        var offsetsToCommit = new Dictionary<int, TopicPartitionOffset>();
                        for (var i=itemsToExecute.Length - 1; i >= 0; i--)
                        {
                            var currentItem = itemsToExecute[i];
                            if (!offsetsToCommit.ContainsKey(currentItem.Partition))
                            {
                                offsetsToCommit.Add(
                                    currentItem.Partition, 
                                    new TopicPartitionOffset(
                                        currentItem.Topic,
                                        currentItem.Partition,
                                        currentItem.Offset + 1)); // offset is inclusive when resuming
                            }
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Commit(offsetsToCommit.Values);

                            if (functionResult.Succeeded)
                            {
                                if (logger.IsEnabled(LogLevel.Debug))
                                {
                                    logger.LogDebug("Function executed with {batchSize} items in {topic} / {partitions} / {offsets}",
                                        itemsToExecute.Length,
                                        itemsToExecute[0].Topic,
                                        string.Join(",", offsetsToCommit.Keys),
                                        string.Join(",", offsetsToCommit.Values.Select(x => x.Offset)));
                                }
                            }
                            else
                            {
                                logger.LogError(functionResult.Exception, "Failed to executed function with {batchSize} items in {topic} / {partitions} / {offsets}",
                                    itemsToExecute.Length,
                                    itemsToExecute[0].Topic,
                                    string.Join(",", offsetsToCommit.Keys),
                                    string.Join(",", offsetsToCommit.Values.Select(x => x.Offset)));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error in executor reader");
                    }
                }
            }

            logger.LogInformation("Exiting reader {processName}", nameof(MultipleItemFunctionExecutor<TKey, TValue>));
        }
    }
}
