// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Threading;
using System.Threading.Tasks;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Kafka listener.
    /// Connects a Kafka trigger function with a Kafka Consumer
    /// </summary>
    internal class KafkaListenerAvro<TKey, TValue> : KafkaListener<TKey, TValue>
    {
        private readonly string avroSchema;

        public KafkaListenerAvro(
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            string avroSchema,
            ILogger logger) : base(executor, singleDispatch, options, brokerList, topic, consumerGroup, eventHubConnectionString, logger)
        {
            this.avroSchema = avroSchema;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            var schemaRegistry = new LocalSchemaRegistry(avroSchema);
            AvroDeserializer<TValue> avroDeserializer = new AvroDeserializer<TValue>(schemaRegistry);
            SetConsumerAndExecutor(avroDeserializer, null, null);

            return Task.CompletedTask;
        }
    }
}