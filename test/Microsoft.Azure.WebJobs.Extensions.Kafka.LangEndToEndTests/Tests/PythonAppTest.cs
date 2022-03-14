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
            string reqMsg = "Single-Event";
            string url = "http://localhost:" + Constants.PYTHONAPP_CONFLUENT_PORT + "/api/" + Constants.PYTHON_SINGLE_APP_NAME;
            
            Dictionary<string, string> reqParm = new Dictionary<string, string>();
            reqParm.TryAdd("message", reqMsg);
            
            HttpRequestEntity httpRequestEntity = new HttpRequestEntity(url, HttpMethods.Get,
               null, reqParm, null);

            List<string> expectedOutput = new List<string> { reqMsg };
            
            await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, expectedOutput);

            //Console.WriteLine("Python test called");
        }

        //[Fact]
        //public async Task Python_App_Test_Multi_Event_Confluent()
        //{
        //    int port = 7071;
        //    string reqMsg = "Multi-Event";
        //    string url = "http://localhost:" + port + "/" + appName;
        //    Dictionary<string, string> reqParm = new Dictionary<string, string>();
        //    reqParm.TryAdd("message", reqMsg);
        //    HttpRequestEntity httpRequestEntity = new HttpRequestEntity(url, HttpMethods.Get,
        //       null, reqParm, null);
        //    Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null);
        //    Console.WriteLine("Python test called");
        //}

    }
}
