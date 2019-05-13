// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Asyncronous commit strategy
    /// </summary>
    public class AsyncCommitStrategy<TKey, TValue> : ICommitStrategy<TKey, TValue>
    {
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly ILogger logger;

        public AsyncCommitStrategy(IConsumer<TKey, TValue> consumer, ILogger logger)
        {
            this.consumer = consumer;
            this.logger = logger;
        }

        public void Commit(IEnumerable<TopicPartitionOffset> topicPartitionOffsets)
        {
            foreach (var tpo in topicPartitionOffsets)
            {
                this.consumer.StoreOffset(tpo);

                this.logger.LogInformation("Stored commit offset {topic} / {partition} / {offset}",
                    tpo.Topic,
                    tpo.Partition,
                    tpo.Offset);
            }
        }
    }
}
