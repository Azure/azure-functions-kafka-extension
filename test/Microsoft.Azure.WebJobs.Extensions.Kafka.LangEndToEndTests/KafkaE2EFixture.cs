using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.cleanup;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.helper;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.initializer;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
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

        public KafkaE2EFixture()
        {
            Console.WriteLine("Kafkae2efixture");
        }

        public void OrchestrateInitialization()
        {
            if(!isInitialized)
            {
                TestSuitInitializer testSuitInitializer = new TestSuitInitializer();
                //Infra setup + Func Apps Startup Start
                testSuitInitializer.InitializeTestSuit(language, brokerType);
                isInitialized = true;
            }
        }

        async Task IAsyncLifetime.DisposeAsync()
        {

            TestSuiteCleaner testSuitCleaner = new TestSuiteCleaner();
            await testSuitCleaner.CleanupTestSuiteAsync(language, brokerType);
            Console.WriteLine("DisposeAsync");
        }

        Task IAsyncLifetime.InitializeAsync()
        {
            Console.WriteLine("InitializeAsync");
            //throw new NotImplementedException();
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
    }
}
