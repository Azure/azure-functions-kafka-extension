// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaObjectTargetScaler : KafkaGenericTargetScaler<Object, Object>
    {
       internal KafkaObjectTargetScaler(string topic, string consumerGroup, 
           KafkaMetricsProvider<Object, Object> metricsProvider, long lagThreshold, ILogger logger) 
            : base(topic, consumerGroup, functionID: null, consumer: null, metricsProvider, lagThreshold, logger)
       { 
       }
    }
}