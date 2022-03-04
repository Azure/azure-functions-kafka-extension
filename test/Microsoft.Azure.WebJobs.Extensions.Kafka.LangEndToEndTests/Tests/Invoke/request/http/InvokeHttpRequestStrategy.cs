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
    public class InvokeHttpRequestStrategy : InvokeRequestStrategy<HttpResponseMessage>
    {
        //Request:Command<HttpResponse> Response:HttpResponse
        //Execute Request Return Reponse
        private IExecutor<Command<HttpResponseMessage>, HttpResponseMessage> httpCommandExecutor;
        private HttpRequestEntity httpRequestEntity;

        public InvokeHttpRequestStrategy(HttpRequestEntity httpRequestEntity)
        {
            this.httpRequestEntity = httpRequestEntity;
            this.httpCommandExecutor = new HttpCommandExecutor();
        }

        //Why do we need executor and command abs? Why two and not one?
        //Single Http client for language vs test?
        public Task<HttpResponseMessage> InvokeRequestAsync()
        {
            Command<HttpResponseMessage> httpCmd = (new HttpCommand.HttpCommandBuilder()).
                SetHttpRequestEntity(httpRequestEntity).Build();
            return httpCommandExecutor.ExecuteAsync(httpCmd);
        }
    }
}
