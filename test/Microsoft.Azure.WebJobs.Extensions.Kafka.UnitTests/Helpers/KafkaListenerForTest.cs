// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    /// <summary>
    /// Test <see cref="KafkaListener{TKey, TValue}"/>, allowing the creation of a custom <see cref="IConsumer{TKey, TValue}"/>
    /// </summary>
    internal class KafkaListenerForTest<TKey, TValue> : KafkaListener<TKey, TValue>
    {
        IConsumer<TKey, TValue> consumer;

        public KafkaListenerForTest(ITriggeredFunctionExecutor executor, bool singleDispatch, KafkaOptions options, string brokerList, string topic, string consumerGroup, string eventHubConnectionString, string avroSchema, ILogger logger) 
            : base(executor, singleDispatch, options, brokerList, topic, consumerGroup, eventHubConnectionString, avroSchema, logger)
        {
        }

        public void SetConsumer(IConsumer<TKey, TValue> consumer) => this.consumer = consumer;

        protected override IConsumer<TKey, TValue> CreateConsumer(ConsumerConfig config, Action<Consumer<TKey, TValue>, Error> errorHandler, Action<IConsumer<TKey, TValue>, RebalanceEvent> rebalanceHandler, IAsyncDeserializer<TValue> asyncValueDeserializer = null, IDeserializer<TValue> valueDeserializer = null, IAsyncDeserializer<TKey> keyDeserializer = null)
        {
            return this.consumer ??
                base.CreateConsumer(config, errorHandler, rebalanceHandler, asyncValueDeserializer, valueDeserializer, keyDeserializer);
        }
    }
}
