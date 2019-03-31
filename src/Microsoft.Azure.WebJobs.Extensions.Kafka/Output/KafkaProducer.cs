// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka producer
    /// </summary>
    public class KafkaProducer<TKey, TValue> : IKafkaProducer
    {
        private readonly IProducer<TKey, TValue> producer;
        private readonly ILogger logger;

        public KafkaProducer(
            ProducerConfig config,
            string avroSchema)
        {
            this.logger = null;
            var builder = new ProducerBuilder<TKey, TValue>(config);

            IAsyncSerializer<TValue> asyncValueSerializer = null;
            ISerializer<TValue> valueSerializer = null;
            IAsyncSerializer<TKey> keySerializer = null;

            if (!string.IsNullOrEmpty(avroSchema))
            {
                var schemaRegistry = new LocalSchemaRegistry(avroSchema);
                asyncValueSerializer = new AvroSerializer<TValue>(schemaRegistry);
            }
            else
            {
                if (typeof(Google.Protobuf.IMessage).IsAssignableFrom(typeof(TValue)))
                {
                    // protobuf: need to create using reflection due to generic requirements in ProtobufSerializer
                    valueSerializer = (ISerializer<TValue>)Activator.CreateInstance(typeof(ProtobufSerializer<>).MakeGenericType(typeof(TValue)));
                }
            }

            if (asyncValueSerializer != null)
            {
                builder.SetValueSerializer(asyncValueSerializer);
            }
            else if (valueSerializer != null)
            {
                builder.SetValueSerializer(valueSerializer);
            }

            if (keySerializer != null)
            {
                builder.SetKeySerializer(keySerializer);
            }

            this.producer = builder.Build();
        }

        public async Task ProduceAsync(string topic, KafkaEventData item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.Value == null)
            {
                throw new ArgumentException("Message value was not defined");
            }

            if (!(item.Value is TValue typedValue))
            {
                throw new ArgumentException($"Message value is not of the expected type. Expected: {typeof(TValue).Name}. Actual: {item.Value.GetType().Name}");
            }

            var msg = new Message<TKey, TValue>()
            {
                Value = typedValue,
            };

            if (item.Key != null)
            {
                msg.Key = (TKey)item.Key;
            }

            var topicUsed = topic;
            if (string.IsNullOrEmpty(topic))
            {
                topicUsed = item.Topic;
            }

            try
            {
                var deliveryReport = await this.producer.ProduceAsync(topicUsed, msg);

                this.logger?.LogDebug("Message produced on {topic} / {partition} / {offset}", deliveryReport.Topic, (int)deliveryReport.Partition, (long)deliveryReport.Offset);
            }
            catch (Exception ex)
            {
                this.logger?.LogError(ex, "Error producing into {topic}", topicUsed);
            }
        }
    }
}