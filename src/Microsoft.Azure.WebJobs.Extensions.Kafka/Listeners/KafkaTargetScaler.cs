// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    class KafkaTargetScaler : ITargetScaler
    {
        // Initialise variables required

        // Defining constructor
        public KafkaTargetScaler() { }

        // first method in ITargetScaler
        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            var res = await Task.Run(() => GetScaleResult());

            return res;
        }

        TargetScalerResult GetScaleResult()
        {
            // use the context to find out currentTargetValue
            int currentTargetValue = 10;
            // return some hard coded result value
            var res = new TargetScalerResult() { TargetWorkerCount = currentTargetValue };
            return res;
        }

        // the only property of ITargetScaler
        public TargetScalerDescriptor TargetScalerDescriptor { get; }
    }
}