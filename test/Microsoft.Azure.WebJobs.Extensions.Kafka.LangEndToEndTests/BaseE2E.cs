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
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.TestLogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    /* This class acts as the base class for all the language test case classes.
     * Takes care of Initial Orchestration and actual flow of the test.
    */
    public class BaseE2E
    {
        private KafkaE2EFixture kafkaE2EFixture;
        private Language language;
        private BrokerType brokerType;
        private E2ETestInvoker invoker;
        ITestOutputHelper output;
        private readonly ILogger logger = TestLogger.TestLogger.GetTestLogger();

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
            //Send invocation Http request to the function apps 
            await InvokeE2ETest(appType, invokeType, httpRequestEntity, queueEntity);
            
            // wait for the function completion
            await Task.Delay(60000);
            
            // invokation for read from storage
            await VerifyQueueMsgsAsync(expectedOutput, appType);
        }

        private async Task VerifyQueueMsgsAsync(List<string> expectedOutput, AppType appType)
        {
            var storageQueueName = Utils.BuildStorageQueueName(brokerType,
                        appType, language);

			Command<QueueResponse> readQueue;
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

            CollectionAssert.AreEquivalent(expectedOutput, queueMsgs.GetResponseList());
        }

        private async Task InvokeE2ETest(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
            KafkaEntity queueEntity)
        {
            if (httpRequestEntity != null && InvokeType.HTTP == invokeType)
            {
                try 
                { 
                    InvokeRequestStrategy<HttpResponseMessage> invokerHttpReqStrategy = new InvokeHttpRequestStrategy(httpRequestEntity);
                    await this.invoker.Invoke(invokerHttpReqStrategy);
                }
                catch(Exception ex)
                {
                    logger.LogError($"Unable to invoke functions for language:{language} broker:{brokerType} with exception {ex}");
                    throw ex;
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
