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

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests
{
    public class PythonAppTest : BaseE2E, IClassFixture<KafkaE2EFixture>
    {
        private KafkaE2EFixture kafkaE2EFixture;
        private static readonly int port = 7071;
        private static readonly string appName = "pythonapp";
        public PythonAppTest(KafkaE2EFixture kafkaE2EFixture): base(kafkaE2EFixture, Language.PYTHON)
        {
            this.kafkaE2EFixture = kafkaE2EFixture;
        }

        [Fact]
        public async Task Python_App_Test_Single_Event()
        {
            string reqMsg = "har har mahadev";
            string url = "http://localhost:"+port+"/"+appName;
            Dictionary<String, String> reqParm = new Dictionary<string, string>();
            reqParm.TryAdd("message", reqMsg);
            HttpRequestEntity httpRequestEntity = new HttpRequestEntity(url, HttpMethods.Get,
               null, reqParm, null);
            Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null);
            Console.WriteLine("Python test called");
        }

    }
}
