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
                var triggerInput = KafkaTriggerInput.New(kafkaEventData);
                var triggerData = new TriggeredFunctionData
                {
                    TriggerValue = triggerInput,
                };

                await this.ExecuteFunctionAsync(triggerData, cancellationToken);

                if (topicPartition == null)
                {
                    topicPartition = new TopicPartition(kafkaEventData.Topic, partition);
                }

                // Commiting after each function execution plays nicer with function scaler.
                // When processing a large batch of events where the execution of each event takes time
                // it would take Events_In_Batch_For_Partition * Event_Processing_Time to update the current offset.
                // Doing it after each event minimizes the delay
                this.Commit(new[] { new TopicPartitionOffset(topicPartition, kafkaEventData.Offset + 1) });  // offset is inclusive when resuming
            }
        }
    }
}