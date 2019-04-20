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
        internal object ValueSerializer { get; }

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
            this.ValueSerializer = valueSerializer;
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

        public async Task ProduceAsync(string topic, object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var actualItem = (IKafkaEventData)item;
            if (actualItem == null)
            {
                throw new ArgumentException($"Message value is not of the expected type. Expected: {typeof(KafkaEventData<TKey, TValue>).Name}. Actual: {item.GetType().Name}");
            }

            if (actualItem.Value == null)
            {
                throw new ArgumentException("Message value was not defined");
            }
            
            var msg = new Message<TKey, TValue>()
            {
                Value = (TValue)actualItem.Value,
            };

            if (actualItem.Key != null)
            {
                if (!(actualItem.Key is TKey keyValue))
                {
                    throw new ArgumentException($"Key value is not of the expected type. Expected: {typeof(TKey).Name}. Actual: {actualItem.Key.GetType().Name}");
                }

                msg.Key = keyValue;
            }

            var topicUsed = topic;
            if (string.IsNullOrEmpty(topic))
            {
                topicUsed = actualItem.Topic;

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