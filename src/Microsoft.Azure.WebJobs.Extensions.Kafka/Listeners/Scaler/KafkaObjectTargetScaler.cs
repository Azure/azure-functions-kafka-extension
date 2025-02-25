// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaObjectTargetScaler : KafkaGenericTargetScaler<string, string>
    {
       internal KafkaObjectTargetScaler(string topic, string consumerGroup,
           KafkaMetricsProvider<string, string> metricsProvider, string functionId, long lagThreshold, ILogger logger) 
            : base(topic, consumerGroup, functionId, consumer: null, metricsProvider, lagThreshold, logger)
       { 
       }
    }
}