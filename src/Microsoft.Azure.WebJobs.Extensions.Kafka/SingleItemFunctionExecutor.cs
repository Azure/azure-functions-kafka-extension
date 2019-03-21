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
    /// Executes the functions for an specific partition
    /// Used for functions that are expecting a single item
    /// </summary>
    public class SingleItemFunctionExecutor<TKey, TValue> : FunctionExecutorBase<TKey, TValue>
    {
        public SingleItemFunctionExecutor(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, CancellationToken cancellationToken, ILogger logger) 
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
                        foreach (var kafkaEventData in itemsToPublish)
                        {
                            var triggerInput = KafkaTriggerInput.New(kafkaEventData);
                            var triggerData = new TriggeredFunctionData
                            {
                                TriggerValue = triggerInput,
                            };

                            var functionResult = await this.ExecuteFunctionAsync(triggerData, cancellationToken);
                            if (functionResult.Succeeded)
                            {

                                logger.LogDebug("Executed {topic} / {partition} / {offset}",
                                    kafkaEventData.Topic,
                                    kafkaEventData.Partition,
                                    kafkaEventData.Offset);
                            }
                            else
                            {
                                logger.LogError(functionResult.Exception, "Failed to execute function {topic} / {partition} / {offset}",
                                    kafkaEventData.Topic,
                                    kafkaEventData.Partition,
                                    kafkaEventData.Offset);
                            }
                        }

                        this.Commit(itemsToPublish.Last());
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