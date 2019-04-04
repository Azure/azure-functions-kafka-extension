// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
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
            string avroSchema,
            ILogger logger)
        {
            this.logger = logger;
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

            producer = builder.Build();
        }

        public void Flush(CancellationToken cancellationToken)
        {
            try
            {
                producer.Flush(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // cancellationToken has been cancelled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error flushing Kafka producer");
                throw;
            }
        }

        public void Produce(string topic, KafkaEventData item)
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
                if (!(item.Key is TKey keyValue))
                {
                    throw new ArgumentException($"Key value is not of the expected type. Expected: {typeof(TKey).Name}. Actual: {item.Key.GetType().Name}");
                }

                msg.Key = keyValue;
            }

            var topicUsed = topic;
            if (string.IsNullOrEmpty(topic))
            {
                topicUsed = item.Topic;

                if (string.IsNullOrEmpty(topicUsed))
                {
                    throw new ArgumentException("No topic was defined in Kafka attribute or in KafkaEventData");
                }
            }

            try
            {
                producer.BeginProduce(topicUsed, msg, DeliveryHandler);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error producing into {topic}", topicUsed);
                throw;
            }
        }

        private void DeliveryHandler(DeliveryReport<TKey, TValue> deliveredItem)
        {
            if (deliveredItem.Error == null || deliveredItem.Error.Code == ErrorCode.NoError)
            {
                logger.LogDebug("Message delivered on {topic} / {partition} / {offset}", deliveredItem.Topic, (int)deliveredItem.Partition, (long)deliveredItem.Offset);
            }
            else
            {
                logger.LogError("Failed to delivery message to {topic} / {partition} / {offset}. Reason: {reason}. Full Error: {error}", deliveredItem.Topic, (int)deliveredItem.Partition, (long)deliveredItem.Offset, deliveredItem.Error.Reason, deliveredItem.Error.ToString());
            }
        }
    }
}