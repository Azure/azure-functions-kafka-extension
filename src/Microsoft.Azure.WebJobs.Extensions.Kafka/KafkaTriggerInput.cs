namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaTriggerInput
    {
        // If != -1, then only process a single event in this batch. 
        private int _selector = -1;

        internal KafkaEventData[] Events { get; set; }

        public bool IsSingleDispatch
        {
            get
            {
                return _selector != -1;
            }
        }

        public static KafkaTriggerInput New(KafkaEventData eventData)
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

        public static KafkaTriggerInput New(KafkaEventData[] eventDataCollection)
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

        public KafkaEventData GetSingleEventData()
        {
            return this.Events[this._selector];
        }
    }
}