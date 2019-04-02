// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Avro.Specific;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Listeners
{
    internal static class KafkaListenerFactory
    {
        public static IListener CreateFor(KafkaTriggerAttribute attribute,
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            ILogger logger)
        {
            var valueType = GetValueType(attribute, out string avroSchema);
            return (IListener)typeof(KafkaListenerFactory)
                .GetMethod(nameof(CreateFor), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod)
                .MakeGenericMethod(attribute.KeyType ?? typeof(Ignore), valueType)
                .Invoke(null, new object[]
                {
                    executor,
                    singleDispatch,
                    options,
                    brokerList,
                    topic,
                    consumerGroup,
                    eventHubConnectionString,
                    logger, avroSchema
                });
        }

        private static Type GetValueType(KafkaTriggerAttribute attribute, out string avroSchema)
        {
            avroSchema = null;

            var valueType = attribute.ValueType;
            if (valueType == null)
            {
                if (!string.IsNullOrEmpty(attribute.AvroSchema))
                {
                    avroSchema = attribute.AvroSchema;
                    return typeof(Avro.Generic.GenericRecord);
                }
                else
                {
                    return typeof(string);
                }
            }
            else
            {
                if (typeof(ISpecificRecord).IsAssignableFrom(valueType))
                {
                    var specificRecord = (ISpecificRecord)Activator.CreateInstance(valueType);
                    avroSchema = specificRecord.Schema.ToString();
                }
            }

            return valueType;
        }

        private static KafkaListener<TKey, TValue> CreateFor<TKey, TValue>(
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            ILogger logger,
            string avroSchema = null)
        {
            if (typeof(ISpecificRecord).IsAssignableFrom(typeof(TValue)))
            {
                if (string.IsNullOrWhiteSpace(avroSchema))
                {
                    throw new ArgumentNullException(nameof(avroSchema), $@"parameter is required when creating an Avro-based Listener");
                }

                return new KafkaListenerAvro<TKey, TValue>(executor,
                    singleDispatch,
                    options,
                    brokerList,
                    topic,
                    consumerGroup,
                    eventHubConnectionString,
                    avroSchema,
                    logger);
            }

            if (typeof(Google.Protobuf.IMessage).IsAssignableFrom(typeof(TValue)))
            {
                return new KafkaListenerProtoBuf<TKey, TValue>(executor,
                    singleDispatch,
                    options,
                    brokerList,
                    topic,
                    consumerGroup,
                    eventHubConnectionString,
                    logger);
            }

            return new KafkaListener<TKey, TValue>(executor,
                    singleDispatch,
                    options,
                    brokerList,
                    topic,
                    consumerGroup,
                    eventHubConnectionString,
                    logger);
        }
    }
}
