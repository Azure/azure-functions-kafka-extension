// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaEventData<TKey, TValue> : IKafkaEventData
    {
        public TKey Key { get; set; }
        public long Offset { get; set; }
        public int Partition { get; set; }
        public string Topic { get; set; }
        public DateTime Timestamp { get; set; }
        public TValue Value { get; set; }

        object IKafkaEventData.Value => this.Value;

        object IKafkaEventData.Key => this.Key;

        public KafkaEventData()
        {
        }

        public KafkaEventData(TKey key, TValue value)
        {
            this.Key = key;
            this.Value = value;
        }

        public KafkaEventData(IKafkaEventData src)
        {
            this.Key = (TKey)src.Key;
            this.Value = (TValue)src.Value;
            this.Offset = src.Offset;
            this.Partition = src.Partition;
            this.Timestamp = src.Timestamp;
            this.Topic = src.Topic;
        }

        public KafkaEventData(ConsumeResult<TKey, TValue> consumeResult)
        {
            this.Key = consumeResult.Key;
            this.Value = consumeResult.Value;
            this.Offset = consumeResult.Offset;
            this.Partition = consumeResult.Partition;
            this.Timestamp = consumeResult.Timestamp.UtcDateTime;
            this.Topic = consumeResult.Topic;
        }
    }


    public class KafkaEventData<TValue> : IKafkaEventData
    {
        public long Offset { get; set; }
        public int Partition { get; set; }
        public string Topic { get; set; }
        public DateTime Timestamp { get; set; }
        public TValue Value { get; set; }

        object IKafkaEventData.Value => this.Value;

        object IKafkaEventData.Key => null;

        public KafkaEventData()
        {
        }

        public KafkaEventData(TValue value)
        {
            this.Value = value;
        }

        public KafkaEventData(IKafkaEventData src)
        {
            this.Value = (TValue)src.Value;
            this.Offset = src.Offset;
            this.Partition = src.Partition;
            this.Timestamp = src.Timestamp;
            this.Topic = src.Topic;
        }

        public KafkaEventData(ConsumeResult<Null, TValue> consumeResult)
        {
            this.Value = consumeResult.Value;
            this.Offset = consumeResult.Offset;
            this.Partition = consumeResult.Partition;
            this.Timestamp = consumeResult.Timestamp.UtcDateTime;
            this.Topic = consumeResult.Topic;
        }
    }
}