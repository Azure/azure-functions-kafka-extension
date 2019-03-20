// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class ConsumeResultWrapper<TKey, TValue> : IConsumeResultData
    {
        private readonly ConsumeResult<TKey, TValue> value;

        public ConsumeResultWrapper(ConsumeResult<TKey, TValue> value)
        {
            this.value = value ?? throw new System.ArgumentNullException(nameof(value));
        }

        public object Key => this.value.Key;

        public string Topic => this.value.Topic;

        public int Partition => this.value.Partition;

        public long Offset => this.value.Offset;

        public DateTime Timestamp => this.value.Timestamp.UtcDateTime;

        public object Value => this.value.Value;
    }
}