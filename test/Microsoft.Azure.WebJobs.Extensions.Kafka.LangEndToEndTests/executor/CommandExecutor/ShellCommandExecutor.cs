// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor
{
    /* Executor for Shell Commands
    */
    public class ShellCommandExecutor : IExecutor<Command<Process>, Process>
    {
        public Task<Process> ExecuteAsync(Command<Process> request)
        {
           return request.ExecuteCommandAsync();
        }
    }
}
