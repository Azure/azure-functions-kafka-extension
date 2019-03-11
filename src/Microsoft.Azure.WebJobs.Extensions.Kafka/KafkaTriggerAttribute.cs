using Avro.Specific;
using Microsoft.Azure.WebJobs.Description;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Trigger attribute to start execution of function when Kafka messages are received
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    [Binding]
    public class KafkaTriggerAttribute : Attribute
    {

        public KafkaTriggerAttribute(string brokerList, string topic)
        {
            this.BrokerList = brokerList;
            this.Topic = topic;
        }

        /// <summary>
        /// Gets or sets the topic
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        /// Gets or sets the broker list
        /// </summary>

        public string BrokerList { get; private set; }

        /// <summary>
        /// Get or sets the EventHub connection string when using Kafka protocol header feature of Azure EventHubs
        /// </summary>
        public string EventHubConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the consumer group
        /// </summary>
        public string ConsumerGroup { get; set; }

        /// <summary>
        /// Gets or sets the key element type
        /// Default is long
        /// </summary>
        public Type KeyType { get; set; }

        Type valueType;

        /// <summary>
        /// Gets or sets the Avro data type
        /// Must implement ISpecificRecord
        /// </summary>
        public Type ValueType
        { 
            get => this.valueType;
            set
            {
                if (value != null && !IsValidValueType(value))
                    throw new ArgumentException($"The value of {nameof(ValueType)} must be a byte[], string or a type that implements {nameof(ISpecificRecord)} or {nameof(Google.Protobuf.IMessage)}. The type {value.Name} does not.");

                this.valueType = value;
            }
        }

        /// <summary>
        /// Gets or sets the Avro schema.
        /// Should be used only if a generic record should be generated
        /// </summary>
        public string AvroSchema { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the batch.
        /// </summary>
        /// <value>The maximum size of the batch.</value>
        public int MaxBatchSize { get; set; } = 64;

        bool IsValidValueType(Type value)
        {
            return
                typeof(ISpecificRecord).IsAssignableFrom(value) ||
                typeof(Google.Protobuf.IMessage).IsAssignableFrom(value) ||
                value == typeof(byte[]) ||
                value == typeof(string);
        }
    }
}