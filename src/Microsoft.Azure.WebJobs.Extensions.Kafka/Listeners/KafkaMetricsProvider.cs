// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaMetricsProvider
    {

        public KafkaMetricsProvider() { }

        // public KafkaMetricsProvider(string functionID, IEventHubConsumerClient client, ...)
        /*
        public async Task<KafkaMetricsProvider> GetMetricsAsync()
        {
            KafkaTriggerMetrics metrics = new KafkaTriggerMetrics();

            return CreateKafkaTriggerMetrics();
        }
        
        private KakfaTriggerMetrics CreateKafkaTriggerMetrics()
        {
            //return new KafkaTriggerMetrics();
            return null;
        }

        private static long GetUnprocessedEventCount()
        {
            return 1;
        }
        */
    }
}