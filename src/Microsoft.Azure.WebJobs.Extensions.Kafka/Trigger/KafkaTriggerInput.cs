// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTriggerInput<TKey, TValue>
    {
        // If != -1, then only process a single event in this batch. 
        private int _selector = -1;

        internal KafkaEventData<TKey, TValue>[] Events { get; set; }

        public bool IsSingleDispatch
        {
            get
            {
                return _selector != -1;
            }
        }

        public static KafkaTriggerInput<TKey, TValue> New(KafkaEventData<TKey, TValue> eventData)
        {
            return new KafkaTriggerInput<TKey, TValue>
            {
                Events = new[]
                {
                      eventData
                },
                _selector = 0,
            };
        }

        public static KafkaTriggerInput<TKey, TValue> New(KafkaEventData<TKey, TValue>[] eventDataCollection)
        {
            return new KafkaTriggerInput<TKey, TValue>
            {
                Events = eventDataCollection,
                _selector = -1,
            };
        }

        public KafkaTriggerInput<TKey, TValue> GetSingleEventTriggerInput(int idx)
        {
            return new KafkaTriggerInput<TKey, TValue>
            {
                Events = this.Events,
                _selector = idx
            };
        }

        public KafkaEventData<TKey, TValue> GetSingleEventData()
        {
            return this.Events[this._selector];
        }
    }
}