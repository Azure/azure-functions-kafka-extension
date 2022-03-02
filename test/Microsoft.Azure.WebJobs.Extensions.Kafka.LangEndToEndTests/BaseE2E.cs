using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request.http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.Type;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xunit.Sdk;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    public class BaseE2E
    {
        private KafkaE2EFixture kafkaE2EFixture;
        private Language language;
        private E2ETestInvoker invoker;

        protected BaseE2E(KafkaE2EFixture kafkaE2EFixture, Language language)
        {
            this.kafkaE2EFixture = kafkaE2EFixture;
            this.language = language;
            this.kafkaE2EFixture.SetLanguage(language);
            this.invoker = new E2ETestInvoker();
            this.kafkaE2EFixture.OrchestrateInitialization();
        }

        public void Test(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
            KafkaEntity queueEntity)
        {
            invokeE2ETest(appType, invokeType, httpRequestEntity, queueEntity);

            Console.WriteLine("A");
        }

        private void invokeE2ETest(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
            KafkaEntity queueEntity)
        {
            if (httpRequestEntity != null && InvokeType.HTTP == invokeType)
            {
                InvokeRequestStrategy<HttpResponse> invokerHttpReqStrategy = new InvokeHttpRequestStrategy(httpRequestEntity);
                this.invoker.Invoke(invokerHttpReqStrategy);

            }
            else
            {
                InvokeRequestStrategy<string> invokerHttpReqStrategy = new InvokeKafkaRequestStrategy("");
                this.invoker.Invoke(invokerHttpReqStrategy);
            }
        }

    }
}
