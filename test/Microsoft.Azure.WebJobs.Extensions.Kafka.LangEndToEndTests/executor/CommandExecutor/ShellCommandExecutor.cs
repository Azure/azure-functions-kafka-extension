using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor
{
    public class ShellCommandExecutor : IExecutor<Command<Process>, Process>
    {
        public Process Execute(Command<Process> request)
        {
           return request.ExecuteCommand();
        }
    }
}
