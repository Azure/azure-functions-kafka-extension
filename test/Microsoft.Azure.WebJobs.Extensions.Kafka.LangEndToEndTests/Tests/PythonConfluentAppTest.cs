using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.Type;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests
{
    public class PythonConfluentAppTest : BaseE2E, IClassFixture<KafkaE2EFixture>
    {
        private KafkaE2EFixture kafkaE2EFixture;
        ITestOutputHelper output;

        public PythonConfluentAppTest(KafkaE2EFixture kafkaE2EFixture, ITestOutputHelper output) : base(kafkaE2EFixture, Language.PYTHON, BrokerType.CONFLUENT, output)
        {
            this.kafkaE2EFixture = kafkaE2EFixture;
            this.output = output;
        }

        [Fact]
        public async Task Python_App_Test_Single_Event_Confluent()
        {
            //Generate Random Guids
            List<string> reqMsgs = Utils.GenerateRandomMsgs(AppType.SINGLE_EVENT);

            //Create HttpRequestEntity with url and query parameters
            HttpRequestEntity httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.PYTHONAPP_CONFLUENT_PORT, Constants.PYTHON_MULTI_APP_NAME, reqMsgs);

            //Test e2e flow with trigger httpRequestEntity and expectedOutcome
            await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
            
        }

        [Fact]
        public async Task Python_App_Test_Multi_Event_Confluent()
        {
            //Generate Random Guids
            List<string> reqMsgs = Utils.GenerateRandomMsgs(AppType.BATCH_EVENT);

            //Create HttpRequestEntity with url and query parameters
            var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.PYTHONAPP_CONFLUENT_PORT, Constants.PYTHON_MULTI_APP_NAME, reqMsgs);

            //Test e2e flow with trigger httpRequestEntity and expectedOutcome
            await Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
        }

    }
}
