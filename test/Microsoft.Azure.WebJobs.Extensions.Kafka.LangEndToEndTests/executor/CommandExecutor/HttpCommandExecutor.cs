using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor
{
    /* Executor for Http Commands
    */
    public class HttpCommandExecutor : IExecutor<Command<HttpResponseMessage>, HttpResponseMessage>
    {
        public Task<HttpResponseMessage> ExecuteAsync(command.Command<HttpResponseMessage> request)
        {
            return request.ExecuteCommandAsync();
        }
    }

}
