using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request.http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.Type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    public class BaseE2E
    {
        private KafkaE2EFixture kafkaE2EFixture;
        private Language language;
        private BrokerType brokerType;
        private E2ETestInvoker invoker;
        ITestOutputHelper output;
        protected BaseE2E(KafkaE2EFixture kafkaE2EFixture, Language language, BrokerType brokerType, ITestOutputHelper output)
        {
            this.kafkaE2EFixture = kafkaE2EFixture;
            this.language = language;
            this.kafkaE2EFixture.SetLanguage(language);
            this.brokerType = brokerType;
            this.kafkaE2EFixture.SetBrokerType(brokerType);
            this.invoker = new E2ETestInvoker();
            this.kafkaE2EFixture.OrchestrateInitialization();
            this.output = output;
        }

        public async Task Test(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
            KafkaEntity queueEntity, List<string> expectedOutput)
        {
            await invokeE2ETest(appType, invokeType, httpRequestEntity, queueEntity);
            // wait for the function completion
            // invokation for read from storage
            await verifyQueueMsgsAsync(expectedOutput, appType);
            
        }

        private async Task verifyQueueMsgsAsync(List<string> expectedOutput, AppType appType)
        {
            var storageQueueName = Utils.BuildStorageQueueName(QueueType.AzureStorageQueue,
                        AppType.SINGLE_EVENT, language);

            Command<QueueResponse> readQueue = null;
            if (AppType.BATCH_EVENT == appType)
            {
                readQueue = new QueueCommand(QueueType.AzureStorageQueue,
                        QueueOperation.READMANY, storageQueueName);
            }
            else
            {
                readQueue = new QueueCommand(QueueType.AzureStorageQueue,
                        QueueOperation.READ, storageQueueName);
            }
            
            QueueResponse queueMsgs = await readQueue.ExecuteCommandAsync();

            Assert.Equal<List<string>>(expectedOutput, queueMsgs.responseList);
        }

        private async Task invokeE2ETest(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
            KafkaEntity queueEntity)
        {
            if (httpRequestEntity != null && InvokeType.HTTP == invokeType)
            {

                int executionCount = 1;

                if (AppType.BATCH_EVENT == appType)
                {
                    executionCount = Constants.BATCH_MESSAGE_COUNT;
                }
                try { 
                    for (var i = 0; i < executionCount; i++)
                    {
                        InvokeRequestStrategy<HttpResponseMessage> invokerHttpReqStrategy = new InvokeHttpRequestStrategy(httpRequestEntity);
                        await this.invoker.Invoke(invokerHttpReqStrategy);

                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                InvokeRequestStrategy<string> invokerKafkaReqStrategy = new InvokeKafkaRequestStrategy("");
                _ = this.invoker.Invoke(invokerKafkaReqStrategy);
            }
        }

    }
}
