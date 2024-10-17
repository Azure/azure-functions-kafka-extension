// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaObjectTopicScaler : KafkaGenericTopicScaler<string, string>
    {
        internal KafkaObjectTopicScaler(string topic, string consumerGroup, 
            KafkaMetricsProvider<string, string> metricsProvider, string functionId, long lagThreshold, ILogger logger) 
            : base(topic, consumerGroup, functionId, consumer: null, metricsProvider, lagThreshold, logger) 
        { 
        }
    }
}