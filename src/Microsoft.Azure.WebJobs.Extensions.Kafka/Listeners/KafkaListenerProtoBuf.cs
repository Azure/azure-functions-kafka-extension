// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka listener.
    /// Connects a Kafka trigger function with a Kafka Consumer
    /// </summary>
    internal class KafkaListenerProtoBuf<TKey, TValue> : KafkaListener<TKey, TValue>
    {
        public KafkaListenerProtoBuf(
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            ILogger logger) : base(executor, singleDispatch, options, brokerList, topic, consumerGroup, eventHubConnectionString, logger) { }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // protobuf: need to create using reflection due to generic requirements in ProtobufDeserializer
            var valueDeserializer = (IDeserializer<TValue>)Activator.CreateInstance(typeof(ProtobufDeserializer<>).MakeGenericType(typeof(TValue)));
            SetConsumerAndExecutor(null, valueDeserializer, null);

            return Task.CompletedTask;
        }
    }
}