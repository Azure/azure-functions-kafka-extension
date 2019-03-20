// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka message publisher.
    /// </summary>
    public interface IKafkaMessagePublisher<TKey, TValue>
    {
        void Publish(ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken);
    }
}