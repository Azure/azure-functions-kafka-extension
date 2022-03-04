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
        private static readonly int BATCH_MESSAGE_COUNT = 5;

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
            // wait for the function completion
            // invokation for read from storage
            Console.WriteLine("A");
        }

        private void invokeE2ETest(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
            KafkaEntity queueEntity)
        {
            if (httpRequestEntity != null && InvokeType.HTTP == invokeType)
            {
                
                int executionCount = 1;
                
                
                //if AppType == Single
                //executionCount = 1 and execute loop once
                //So that single msgs are produced into kafka topic
                
                //else AppType == Batch_Event
                //executionCount = Batch_Message_Count
                //and loop execution times
                //So that multiple msgs are produced into kafka topic
                
                //Function App 1 
                //Http Trigger + Kafka Output(topic: 1234)

                //Function App 2 
                //Kafka Trigger(Single/Multiple)(topic: 1234) + Queue Output

                if(AppType.BATCH_EVENT == appType)
                {
                    executionCount = BATCH_MESSAGE_COUNT;
                }

                for (var i = 0; i < executionCount; i++)
                {
                    InvokeRequestStrategy<HttpResponseMessage> invokerHttpReqStrategy = new InvokeHttpRequestStrategy(httpRequestEntity);
                    this.invoker.Invoke(invokerHttpReqStrategy);
                }

            }
            else
            {
                InvokeRequestStrategy<string> invokerHttpReqStrategy = new InvokeKafkaRequestStrategy("");
                this.invoker.Invoke(invokerHttpReqStrategy);
            }
        }

    }
}
