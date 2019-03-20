// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Publishes messages as they come, one at a time
    /// </summary>
    public class SingleKafkaMessagePublisher<TKey, TValue> : IKafkaMessagePublisher<TKey, TValue>
    {
        const int CommitPeriod = 5;

        private readonly ITriggeredFunctionExecutor executor;
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly ILogger logger;

        public SingleKafkaMessagePublisher(ITriggeredFunctionExecutor executor, IConsumer<TKey, TValue> consumer, ILogger logger)
        {
            this.executor = executor ?? throw new System.ArgumentNullException(nameof(executor));
            this.consumer = consumer ?? throw new System.ArgumentNullException(nameof(consumer));
            this.logger = logger;
        }

        public void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            var consumeResultData = new ConsumeResultWrapper<TKey, TValue>(consumeResult);
            var triggerInput = KafkaTriggerInput.New(new KafkaEventData(consumeResultData));

            var triggerData = new TriggeredFunctionData
            {
                TriggerValue = triggerInput,
            };

            executor.TryExecuteAsync(triggerData, cancellationToken);

            if (consumeResultData.Offset % CommitPeriod == 0)
            {
                // The Commit method sends a "commit offsets" request to the Kafka
                // cluster and synchronously waits for the response. This is very
                // slow compared to the rate at which the consumer is capable of
                // consuming messages. A high performance application will typically
                // commit offsets relatively infrequently and be designed handle
                // duplicate messages in the event of failure.
                try
                {
                    this.consumer.Commit();

                    this.logger.LogDebug("Committted topic: {topic}, partition: {partition}, offset: {offset}",
                       consumeResultData.Topic,
                       consumeResultData.Partition,
                       consumeResultData.Offset);
                }
                catch (KafkaException e)
                {
                    this.logger.LogError(e, $"Commit error: {e.Error.Reason}");
                }
            }
        }
    }
}