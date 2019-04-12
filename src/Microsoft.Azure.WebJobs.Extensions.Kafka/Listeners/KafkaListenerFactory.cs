// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Avro.Generic;
using Avro.Specific;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class KafkaListenerFactory
    {
        public static IListener CreateFor(KafkaTriggerAttribute attribute,
            Type parameterType,
            ITriggeredFunctionExecutor executor,
            bool singleDispatch,
            KafkaOptions options,
            string brokerList,
            string topic,
            string consumerGroup,
            string eventHubConnectionString,
            ILogger logger)
        {
            var valueType = SerializationHelper.GetValueType(attribute.ValueType, attribute.AvroSchema, parameterType, out var avroSchema);
            var valueDeserializer = SerializationHelper.ResolveValueDeserializer(valueType, avroSchema);

            return (IListener)Activator.CreateInstance(
                typeof(KafkaListener<,>).MakeGenericType(attribute.KeyType ?? typeof(Ignore), valueType),
                new object[]
                {
                    executor,
                    singleDispatch,
                    options,
                    brokerList,
                    topic,
                    consumerGroup,
                    eventHubConnectionString,
                    valueDeserializer,
                    logger
                });
        }
    }
}
