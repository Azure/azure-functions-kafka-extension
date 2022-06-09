using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor
{
    public class ShellCommandExecutor : IExecutor<Command<Process>, Process>
    {
        public Task<Process> ExecuteAsync(Command<Process> request)
        {
           return request.ExecuteCommandAsync();
        }
    }
}
