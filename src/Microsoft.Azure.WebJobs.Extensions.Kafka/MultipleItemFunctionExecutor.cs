// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
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
        public MultipleItemFunctionExecutor(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, CancellationToken cancellationToken, ILogger logger) 
            : base(executor, consumer, cancellationToken, logger)
        {
        }

        protected override async Task ReaderAsync(ChannelReader<KafkaEventData[]> reader, CancellationToken cancellationToken, ILogger logger)
        {
            while (await reader.WaitToReadAsync(cancellationToken))
            {
                while (reader.TryRead(out var itemsToPublish))
                {
                    try
                    {
                        // Try to publish them
                        var triggerInput = KafkaTriggerInput.New(itemsToPublish);
                        var triggerData = new TriggeredFunctionData
                        {
                            TriggerValue = triggerInput,
                        };


                        var functionResult = await this.ExecuteFunctionAsync(triggerData, cancellationToken);
                        if (functionResult.Succeeded)
                        {

                            logger.LogDebug("Executed {batchSize} items in {topic} / {partition} / {startingOffset}-{endingOffset}",
                                itemsToPublish.Length,
                                itemsToPublish[0].Topic,
                                itemsToPublish[0].Partition,
                                itemsToPublish[0].Offset,
                                itemsToPublish[itemsToPublish.Length - 1].Offset);

                            this.Commit(itemsToPublish.Last());
                        }
                        else
                        { 
                            logger.LogError(functionResult.Exception, "Failed to executed function with {batchSize} items in {topic} / {partition} / {startingOffset}-{endingOffset}",
                                itemsToPublish.Length,
                                itemsToPublish[0].Topic,
                                itemsToPublish[0].Partition,
                                itemsToPublish[0].Offset,
                                itemsToPublish[itemsToPublish.Length - 1].Offset);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Error in partition publisher reader");
                    }
                }
            }
        }
    }
}
