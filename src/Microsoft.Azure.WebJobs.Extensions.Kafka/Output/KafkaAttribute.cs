// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Avro.Specific;
using Microsoft.Azure.WebJobs.Description;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Setup an 'output' binding to an Kafka topic. This can be any output type compatible with an IAsyncCollector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    [Binding]
    public sealed class KafkaAttribute : Attribute
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="KafkaAttribute"/>
        /// </summary>
        /// <param name="topic">Topic name</param>
        public KafkaAttribute(string topic)
        {
            this.Topic = topic;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Azure.WebJobs.Extensions.Kafka.KafkaAttribute"/> class.
        /// </summary>
        public KafkaAttribute()
        {
        }

        /// <summary>
        /// The topic name hub.
        /// </summary>
        [AutoResolve]
        public string Topic { get; private set; }

        /// <summary>
        /// Gets or sets the Broker List.
        /// </summary>
        // [ConnectionString]
        public string BrokerList { get; set; }


        /// <summary>
        /// Gets or sets the key element type
        /// Default is long
        /// </summary>
        public Type KeyType { get; set; }

        private Type valueType;

        /// <summary>
        /// Gets or sets the Avro data type
        /// Must implement ISpecificRecord
        /// </summary>
        public Type ValueType
        {
            get => valueType;
            set
            {
                if (value != null && !IsValidValueType(value))
                {
                    throw new ArgumentException($"The value of {nameof(this.ValueType)} must be a byte[], string or a type that implements {nameof(ISpecificRecord)} or {nameof(Google.Protobuf.IMessage)}. The type {value.Name} does not.");
                }

                valueType = value;
            }
        }

        /// <summary>
        /// Gets or sets the Avro schema.
        /// Should be used only if a generic record should be generated
        /// </summary>
        public string AvroSchema { get; set; }

        private bool IsValidValueType(Type value)
        {
            return
                typeof(ISpecificRecord).IsAssignableFrom(value) ||
                typeof(Google.Protobuf.IMessage).IsAssignableFrom(value) ||
                value == typeof(byte[]) ||
                value == typeof(string);
        }

        /// <summary>
        /// Gets or sets the Maximum transmit message size. Default: 1MB
        /// </summary>
        public int MaxMessageBytes { get; set; } = 1024000;

        /// <summary>
        /// Maximum number of messages batched in one MessageSet. default: 10000
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// When set to `true`, the producer will ensure that messages are successfully produced exactly once and in the original produce order. default: false
        /// </summary>
        public bool EnableIdempotence { get; set; } = false;

        /// <summary>
        /// Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery. A time of 0 is infinite. This is the maximum time used to deliver a message (including retries). Delivery error occurs when either the retry count or the message timeout are exceeded. default: 300000
        /// </summary>
        public int MessageTimeoutMs { get; set; } = 300000;

        /// <summary>
        /// The ack timeout of the producer request in milliseconds. default: 5000
        /// </summary>
        public int RequestTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// How many times to retry sending a failing Message. **Note:** default: 2 
        /// </summary>
        /// <remarks>Retrying may cause reordering unless <c>EnableIdempotence</c> is set to <c>true</c>.</remarks>
        public int MaxRetries { get; set; } = 2;
    }
}