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
    public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer
    {
        private readonly object valueSerializer;
        internal object ValueSerializer => valueSerializer;

        private readonly ILogger logger;
        private IProducer<TKey, TValue> producer;

        /// <summary>
        /// Creates a producer
        /// </summary>
        public KafkaProducer(
            Handle producerHandle,
            object valueSerializer,
            ILogger logger)
        {
            this.valueSerializer = valueSerializer;
            this.logger = logger;
            var builder = new DependentProducerBuilder<TKey, TValue>(producerHandle);

            if (valueSerializer != null)
            {
                if (valueSerializer is IAsyncSerializer<TValue> asyncSerializer)
                {
                    builder.SetValueSerializer(asyncSerializer);
                }
                else if (valueSerializer is ISerializer<TValue> syncSerializer)
                {
                    builder.SetValueSerializer(syncSerializer);
                }
                else
                {
                    throw new ArgumentException($"Value serializer must implement either IAsyncSerializer or ISerializer. Type {valueSerializer.GetType().Name} does not", nameof(valueSerializer));
                }
            }

            this.producer = builder.Build();
        }

        public async Task ProduceAsync(string topic, KafkaEventData item)
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
                var deliveryResult = await this.producer.ProduceAsync(topicUsed, msg);

                this.logger.LogDebug("Message delivered on {topic} / {partition} / {offset}", deliveryResult.Topic, (int)deliveryResult.Partition, (long)deliveryResult.Offset);                
            }
            catch (ProduceException<TKey, TValue> produceException)
            {
                logger.LogError("Failed to delivery message to {topic} / {partition} / {offset}. Reason: {reason}. Full Error: {error}", produceException.DeliveryResult?.Topic, (int)produceException.DeliveryResult?.Partition, (long)produceException.DeliveryResult?.Offset, produceException.Error.Reason, produceException.Error.ToString());
                throw;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error producing into {topic}", topicUsed);
                throw;
            }
        }

        public void Dispose()
        {
            this.producer?.Dispose();
            this.producer = null;
            GC.SuppressFinalize(this);
        }
    }
}