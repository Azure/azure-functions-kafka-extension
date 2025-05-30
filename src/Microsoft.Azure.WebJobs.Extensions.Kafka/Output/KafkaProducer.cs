﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka producer
    /// </summary>
    public sealed class KafkaProducer<TKey, TValue> : IKafkaProducer
    {
        internal object ValueSerializer { get; }

        internal object KeySerializer { get; }

        private readonly ILogger logger;

        internal KafkaMessageBuilder<TKey, TValue> MessageBuilder { get; }

        private IProducer<TKey, TValue> producer;

        /// <summary>
        /// Creates a producer
        /// </summary>
        public KafkaProducer(
            Handle producerHandle,
            object valueSerializer,
            object keySerializer,
            ILogger logger)
        {
            this.ValueSerializer = valueSerializer;
            this.KeySerializer = keySerializer;
            this.logger = logger;
            this.MessageBuilder = new KafkaMessageBuilder<TKey, TValue>();
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

            if (keySerializer != null)
            {
                if (keySerializer is IAsyncSerializer<TKey> asyncSerializer)
                {
                    builder.SetKeySerializer(asyncSerializer);
                }
                else if (keySerializer is ISerializer<TKey> syncSerializer)
                {
                    builder.SetKeySerializer(syncSerializer);
                }
                else
                {
                    throw new ArgumentException($"Key serializer must implement either IAsyncSerializer or ISerializer. Type {keySerializer.GetType().Name} does not", nameof(keySerializer));
                }
            }

            this.producer = builder.Build();
        }

        public async Task ProduceAsync(string topic, object item)
        {
            ValidateItem(item);
            IKafkaEventData actualItem = GetItem(item);
            Message<TKey, TValue> msg = BuildMessage(item, actualItem);
            string topicUsed = FindTopic(topic, actualItem);

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

        public void Produce(string topic, object item)
        {
            ValidateItem(item);
            IKafkaEventData actualItem = GetItem(item);
            Message<TKey, TValue> msg = BuildMessage(item, actualItem);
            string topicUsed = FindTopic(topic, actualItem);

            try
            {
                logger.LogInformation("in Produce method");
                this.producer.Produce(topicUsed, msg, 
                    deliveryResult => {
                        if (deliveryResult.Error.Code != ErrorCode.NoError)
                        {
                            this.logger.LogError("msg failed to deliver on topic :: ", topicUsed + " error :: " + deliveryResult.Error.ToString());
                            return;
                        }
                        this.logger.LogDebug("Message delivered on {topic} / {partition} / {offset}", deliveryResult.Topic, (int)deliveryResult.Partition, (long)deliveryResult.Offset);
                    });
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

        public void Flush()
        {
            this.producer.Flush();
        }

        private static IKafkaEventData GetItem(object item)
        {
            IKafkaEventData actualItem = (IKafkaEventData)item;
            if (actualItem == null)
            {
                throw new ArgumentException($"Message value is not of the expected type. Expected: {typeof(KafkaEventData<TKey, TValue>).Name}. Actual: {item.GetType().Name}");
            }

            return actualItem;
        }

        private static string FindTopic(string topic, IKafkaEventData actualItem)
        {
            var topicUsed = topic;
            if (string.IsNullOrEmpty(topic))
            {
                topicUsed = actualItem.Topic;

                if (string.IsNullOrEmpty(topicUsed))
                {
                    throw new ArgumentException("No topic was defined in Kafka attribute or in KafkaEventData");
                }
            }

            return topicUsed;
        }

        private Message<TKey, TValue> BuildMessage(object item, IKafkaEventData actualItem)
        {
            if (actualItem.Value == null)
            {
                throw new ArgumentException("Message value was not defined");
            }
            return MessageBuilder.BuildFrom(actualItem);
        }

        private static void ValidateItem(object item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
        }

        public void Dispose()
        {
            this.producer?.Flush();
            this.producer?.Dispose();
            this.producer = null;
            GC.SuppressFinalize(this);
        }
    }
}