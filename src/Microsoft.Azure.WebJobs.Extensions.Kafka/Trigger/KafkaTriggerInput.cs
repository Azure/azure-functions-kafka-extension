// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTriggerInput
    {
        // If != -1, then only process a single event in this batch. 
        private int _selector = -1;

        internal IKafkaEventData[] Events { get; set; }

        public bool IsSingleDispatch
        {
            get
            {
                return _selector != -1;
            }
        }

        public static KafkaTriggerInput New(IKafkaEventData eventData)
        {
            return new KafkaTriggerInput
            {
                Events = new[]
                {
                      eventData
                },
                _selector = 0,
            };
        }

        public static KafkaTriggerInput New(IKafkaEventData[] eventDataCollection)
        {
            return new KafkaTriggerInput
            {
                Events = eventDataCollection,
                _selector = -1,
            };
        }

        public KafkaTriggerInput GetSingleEventTriggerInput(int idx)
        {
            return new KafkaTriggerInput
            {
                Events = this.Events,
                _selector = idx
            };
        }

        public IKafkaEventData GetSingleEventData()
        {
            return this.Events[this._selector];
        }
    }
}