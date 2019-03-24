// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Executes the functions for an specific partition.
    /// Used for functions that are expecting multiple items at once
    /// </summary>
    public class MultipleItemFunctionExecutor<TKey, TValue> : FunctionExecutorBase<TKey, TValue>
    {
        public MultipleItemFunctionExecutor(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, int channelCapacity, int channelFullRetryIntervalInMs, ILogger logger) 
            : base(executor, consumer, channelCapacity, channelFullRetryIntervalInMs, logger)
        {
        }

        protected override async Task ReaderAsync(ChannelReader<KafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger)
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

                        var functionResult = await this.ExecuteFunctionAsync(triggerData, cancellationToken);

                        var offsetsToCommit = new Dictionary<int, TopicPartitionOffset>();
                        for (var i=itemsToExecute.Length - 1; i >= 0; i--)
                        {
                            if (!offsetsToCommit.ContainsKey(itemsToExecute[i].Partition))
                            {
                                offsetsToCommit.Add(
                                    itemsToExecute[i].Partition, 
                                    new TopicPartitionOffset(
                                        itemsToExecute[i].Topic,
                                        itemsToExecute[i].Partition,
                                        itemsToExecute[i].Offset + 1)); // offset is inclusive when resuming
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
