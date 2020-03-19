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
    /// Executes the functions for an specific partition
    /// Used for functions that are expecting a single item
    /// </summary>
    public class SingleItemFunctionExecutor<TKey, TValue> : FunctionExecutorBase<TKey, TValue>
    {
        public SingleItemFunctionExecutor(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, int channelCapacity, int channelFullRetryIntervalInMs, ICommitStrategy<TKey, TValue> commitStrategy, ILogger logger)
            : base(executor, consumer, channelCapacity, channelFullRetryIntervalInMs, commitStrategy, logger)
        {
        }

        protected override async Task ReaderAsync(ChannelReader<IKafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger)
        {
            var pendingTasks = new List<Task<FunctionResult>>();

            while (!cancellationToken.IsCancellationRequested && await reader.WaitToReadAsync(cancellationToken))
            {
                while (!cancellationToken.IsCancellationRequested && reader.TryRead(out var itemsToExecute))
                {
                    try
                    {
                        // Execute multiple topic in parallel.
                        // Order in a partition must be followed.
                        var partitionOffsets = new Dictionary<int, long>();
                        var itemsByPartition = itemsToExecute.GroupBy(x => x.Partition);

                        var i = 0;
                        do
                        {
                            pendingTasks.Clear();

                            foreach (var partition in itemsByPartition)
                            {
                                var kafkaEventData = partition.ElementAtOrDefault(i);
                                if (kafkaEventData != null)
                                {
                                    var triggerInput = KafkaTriggerInput.New(kafkaEventData);
                                    var triggerData = new TriggeredFunctionData
                                    {
                                        TriggerValue = triggerInput,
                                    };

                                    partitionOffsets[partition.Key] = kafkaEventData.Offset + 1;  // offset is inclusive when resuming

                                    pendingTasks.Add(this.ExecuteFunctionAsync(triggerData, cancellationToken));
                                }
                            }

                            i++;
                            await Task.WhenAll(pendingTasks);
                            this.Commit(partitionOffsets.Select((kv) => new TopicPartitionOffset(new TopicPartition(itemsToExecute[0].Topic, kv.Key), kv.Value)));
                        } while (!cancellationToken.IsCancellationRequested && pendingTasks.Count > 0);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error in executor reader");
                    }
                }
            }

            logger.LogInformation("Exiting reader {processName}", nameof(SingleItemFunctionExecutor<TKey, TValue>));
        }
    }
}