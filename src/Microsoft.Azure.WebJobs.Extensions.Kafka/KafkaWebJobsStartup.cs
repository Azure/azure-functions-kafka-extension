// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Extensions.Kafka;

[assembly: WebJobsStartup(typeof(KafkaWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddKafka();
        }
    }
}
