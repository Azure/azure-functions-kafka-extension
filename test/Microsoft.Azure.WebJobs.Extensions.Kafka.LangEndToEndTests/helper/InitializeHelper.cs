using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.helper
{
    public class InitializeHelper
    {
        private static InitializeHelper instance = new InitializeHelper();
        private static readonly string E2E = "e2e";
        private static readonly string HIPHEN = "-";
        private static readonly string KAFKA = "KAFKA";

        public static InitializeHelper GetInstance()
        {
            return instance;
        }

        public string BuildCloudBrokerName(QueueType queueType, AppType appType, Language language)
        {
            return E2E + HIPHEN + KAFKA + HIPHEN + language.ToString() + HIPHEN + appType.ToString() + HIPHEN + queueType.ToString();
        }

        public string BuildStorageQueueName(QueueType queueType, AppType appType, Language language)
        {
            return E2E + HIPHEN + language.ToString() + HIPHEN + appType.ToString() + HIPHEN + queueType.ToString();
        }

    }
}
