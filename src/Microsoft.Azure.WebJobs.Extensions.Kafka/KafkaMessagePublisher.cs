// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka message publisher.
    /// </summary>
    public interface IKafkaMessagePublisher<TKey, TValue>
    {
        /// <summary>
        /// Publish events
        /// </summary>
        /// <param name="consumeResult">Consume result.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken);

        /// <summary>
        /// Clears the partitions. Called when they have been revoked
        /// </summary>
        /// <param name="partitions">Partitions.</param>
        void ClearPartitions(IList<TopicPartition> partitions);
        void AddPartitions(IList<TopicPartition> partitions);
    }
}