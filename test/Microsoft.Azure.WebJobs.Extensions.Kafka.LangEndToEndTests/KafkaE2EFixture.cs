using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.cleanup;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.initializer;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    public class KafkaE2EFixture : IAsyncLifetime
    {
        private Language language;
        private AppType appType;
        private BrokerType brokerType;
        protected bool isInitialized = false;
        private readonly ILogger logger = TestLogger.TestLogger.logger;

        public KafkaE2EFixture()
        {
            logger.LogInformation($"Kafkae2efixture for {language} {appType} {brokerType}");
        }

        public void OrchestrateInitialization()
        {
            if (isInitialized)
            {
                return;            
            }

            //Azure Infra setup and Func Apps Startup
            TestSuitInitializer testSuitInitializer = new TestSuitInitializer();
            testSuitInitializer.InitializeTestSuit(language, brokerType);
            
            isInitialized = true;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            TestSuiteCleaner testSuitCleaner = new TestSuiteCleaner();
            await testSuitCleaner.CleanupTestSuiteAsync(language, brokerType);
            logger.LogInformation("DisposeAsync");
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            logger.LogInformation("InitializeAsync");
            return Task.CompletedTask;
        }

        public void SetLanguage(Language language)
        {
            this.language = language;
        }
        public void SetAppType(AppType appType)
        {
            this.appType = appType;
        }
        public void SetBrokerType(BrokerType brokerType)
        {
            this.brokerType = brokerType;
        }

        public AppType GetAppType()
        {
            return this.appType;
        }
    }
}
