using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor
{
    public class HttpCommandExecutor : IExecutor<Command<HttpResponseMessage>, HttpResponseMessage>
    {
        public Task<HttpResponseMessage> ExecuteAsync(command.Command<HttpResponseMessage> request)
        {
            //If we wanted extract the content string from HttpResponse we can add the code here
            return request.ExecuteCommandAsync();
        }
    }

}
