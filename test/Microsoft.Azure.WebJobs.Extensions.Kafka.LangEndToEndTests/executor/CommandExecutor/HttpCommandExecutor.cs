using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor
{
    public class HttpCommandExecutor : IExecutor<Command<HttpResponse>, HttpResponse>
    {
        public HttpResponse Execute(command.Command<HttpResponse> request)
        {
            return request.ExecuteCommand();
        }
    }
}
