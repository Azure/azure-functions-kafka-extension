// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Commit strategy.
    /// </summary>
    public interface ICommitStrategy<TKey, TValue>
    {
        void Commit(IEnumerable<TopicPartitionOffset> topicPartitionOffsets);
    }
}
