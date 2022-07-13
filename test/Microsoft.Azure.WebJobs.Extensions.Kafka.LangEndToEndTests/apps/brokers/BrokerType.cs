using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers
{
    /* Represents the supported Managed Kafka offering for kafka extension.
    */
    public enum BrokerType
    {
        CONFLUENT,
        EVENTHUB
    }
}
