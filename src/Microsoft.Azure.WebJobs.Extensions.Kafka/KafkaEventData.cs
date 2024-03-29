﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaEventData<TKey, TValue> : IKafkaEventData
    {
        public TKey Key { get; set; }
        public long Offset { get; set; }
        public int Partition { get; set; }
        public string Topic { get; set; }
        public IKafkaEventDataHeaders Headers { get; }
        public DateTime Timestamp { get; set; }
        public TValue Value { get; set; }

        object IKafkaEventData.Value => this.Value;

        object IKafkaEventData.Key => this.Key;

        public KafkaEventData()
        {
            this.Headers = new KafkaEventDataHeaders(false);
        }

        public KafkaEventData(TKey key, TValue value) : this()
        {
            this.Key = key;
            this.Value = value;
        }

        public KafkaEventData(ConsumeResult<TKey, TValue> consumeResult)
        {
            this.Key = consumeResult.Key;
            this.Value = consumeResult.Value;
            this.Offset = consumeResult.Offset;
            this.Partition = consumeResult.Partition;
            this.Timestamp = consumeResult.Message.Timestamp.UtcDateTime;
            this.Topic = consumeResult.Topic;
            if (consumeResult.Headers?.Count > 0)
            {
                this.Headers = new KafkaEventDataHeaders(consumeResult.Message.Headers);
            }
            else
            {
                this.Headers = KafkaEventDataHeaders.EmptyReadOnly;
            }
        }
    }

    public class KafkaEventData<TValue> : IKafkaEventData
    {
        public long Offset { get; set; }
        public int Partition { get; set; }
        public string Topic { get; set; }
        public DateTime Timestamp { get; set; }
        public TValue Value { get; set; }

        public object Key { get; set; }

        object IKafkaEventData.Value => this.Value;

        object IKafkaEventData.Key => this.Key;

        public IKafkaEventDataHeaders Headers { get; private set; } 

        public KafkaEventData()
        {
            this.Headers = new KafkaEventDataHeaders(false);
        }

        public KafkaEventData(TValue value) : this()
        {
            this.Value = value;
        }

        internal KafkaEventData(KafkaEventDataHeaders headers)
        {
            this.Headers = headers;
        }

        internal static KafkaEventData<TValue> CreateFrom<TKey>(ConsumeResult<TKey, TValue> consumeResult)
        {
            KafkaEventDataHeaders headers;
            if (consumeResult.Headers?.Count > 0)
            {
                headers = new KafkaEventDataHeaders(consumeResult.Message.Headers);
            }
            else
            {
                headers = KafkaEventDataHeaders.EmptyReadOnly;
            }

            var result = new KafkaEventData<TValue>(headers)
            {
                Value = consumeResult.Value,
                Offset = consumeResult.Offset,
                Partition = consumeResult.Partition,
                Timestamp = consumeResult.Timestamp.UtcDateTime,
                Topic = consumeResult.Topic,
                Key = consumeResult.Key
            };

            return result;
        }

        public KafkaEventData(IKafkaEventData src)
        {
            this.Value = (TValue)src.Value;
            this.Offset = src.Offset;
            this.Partition = src.Partition;
            this.Timestamp = src.Timestamp;
            this.Topic = src.Topic;
            this.Headers = new KafkaEventDataHeaders(src.Headers.Select(x => new KafkaEventDataHeader(x.Key, x.Value)), (src.Headers as KafkaEventDataHeaders)?.IsReadOnly == true);
        }
    }
}