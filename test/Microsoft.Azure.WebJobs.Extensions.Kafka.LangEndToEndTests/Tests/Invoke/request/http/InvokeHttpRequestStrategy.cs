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

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests.Invoke.request.http
{
    public class InvokeHttpRequestStrategy : InvokeRequestStrategy<HttpResponse>
    {
        private IExecutor<Command<HttpResponse>, HttpResponse> httpCommandExecutor;
        private HttpRequestEntity httpRequestEntity;

        public InvokeHttpRequestStrategy(HttpRequestEntity httpRequestEntity)
        {
            this.httpRequestEntity = httpRequestEntity;
            this.httpCommandExecutor = new HttpCommandExecutor();
        }

        public HttpResponse InvokeRequest()
        {
            Command<HttpResponse> httpCmd = (new HttpCommand.HttpCommandBuilder()).
                SetHttpRequestEntity(httpRequestEntity).Build();
            return httpCommandExecutor.Execute(httpCmd);
        }
    }
}
