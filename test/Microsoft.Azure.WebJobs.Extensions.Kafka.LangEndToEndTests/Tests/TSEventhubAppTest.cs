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
    public class TSEventhubAppTest : BaseE2E, IClassFixture<KafkaE2EFixture>
    {
        private KafkaE2EFixture kafkaE2EFixture;
        ITestOutputHelper output;

        public TSEventhubAppTest(KafkaE2EFixture kafkaE2EFixture, ITestOutputHelper output) : base(kafkaE2EFixture, Language.TYPESCRIPT, BrokerType.EVENTHUB, output)
        {
            this.kafkaE2EFixture = kafkaE2EFixture;
            this.output = output;
        }

        [Fact]
        public async Task TS_App_Test_Single_Event_Eventhub()
        {
            string reqMsg = "Single-Event";
            string url = "http://localhost:" + Constants.TSAPP_EVENTHUB_PORT + "/api/" + Constants.TS_SINGLE_APP_NAME;

            Dictionary<string, string> reqParm = new Dictionary<string, string>();
            reqParm.TryAdd("message", reqMsg);

            HttpRequestEntity httpRequestEntity = new HttpRequestEntity(url, HttpMethods.Get,
               null, reqParm, null);

            List<string> expectedOutput = new List<string> { reqMsg };

            await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, expectedOutput);

            //Console.WriteLine("Python test called");
        }


        [Fact]
        public async Task TS_App_Test_Multi_Event_Eventhub()
        {
            string reqMsg1 = "Multi-Event1";
            string reqMsg2 = "Multi-Event2";
            string reqMsg3 = "Multi-Event3";

            string url = "http://localhost:" + Constants.TSAPP_EVENTHUB_PORT + "/api/" + Constants.TS_MULTI_APP_NAME;

            Dictionary<string, string> reqParm = new Dictionary<string, string>();
            reqParm.TryAdd("message", reqMsg1);
            reqParm.TryAdd("message1", reqMsg2);
            reqParm.TryAdd("message2", reqMsg3);

            HttpRequestEntity httpRequestEntity = new HttpRequestEntity(url, HttpMethods.Get,
               null, reqParm, null);

            List<string> expectedOutput = new List<string> { reqMsg1, reqMsg2, reqMsg3 };

            await Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null, expectedOutput);
            //Console.WriteLine("Python test called");
        }
    }
}
