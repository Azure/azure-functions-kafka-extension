using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using System;
using System.Collections.Generic;
using System.Text;

[assembly: WebJobsStartup(typeof(KafkaWebJobsStartup))]

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaWebJobsStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExtension<KafkaExtensionConfigProvider>();
        }
    }
}
