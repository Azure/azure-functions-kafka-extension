// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
    /* Representation of commands needed to be run on shell. 
    */
    public class ShellCommand: Command<Process>
    {
        private Process process;
        protected string cmd;
        private IExecutor<string, Process> processExecutor = null;

        protected ShellCommand()
        {
            processExecutor = new ProcessExecutor();
        }

        public async Task<Process> ExecuteCommandAsync()
        {
            process = await processExecutor.ExecuteAsync(cmd);
            return process;
        }

        public void Dispose() {}

    }
}
