using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request.http
{
    /* Invoke Strategy for http triggered function apps
    */
    public class InvokeHttpRequestStrategy : InvokeRequestStrategy<HttpResponseMessage>
    {
        private IExecutor<Command<HttpResponseMessage>, HttpResponseMessage> httpCommandExecutor;
        private HttpRequestEntity httpRequestEntity;

        public InvokeHttpRequestStrategy(HttpRequestEntity httpRequestEntity)
        {
            this.httpRequestEntity = httpRequestEntity;
            this.httpCommandExecutor = new HttpCommandExecutor();
        }

        public Task<HttpResponseMessage> InvokeRequestAsync()
        {
            Command<HttpResponseMessage> httpCmd = (new HttpCommand.HttpCommandBuilder()).
                SetHttpRequestEntity(httpRequestEntity).Build();
            return httpCommandExecutor.ExecuteAsync(httpCmd);
        }
    }
}
