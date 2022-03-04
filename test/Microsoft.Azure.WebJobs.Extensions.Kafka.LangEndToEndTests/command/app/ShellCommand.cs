using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.process;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
    public class ShellCommand: Command<Process>
    {
        private Process process;
        private Language language;
        private string dockerCmd = "docker run ";
        private string funcAppCmd = "func start";
        private bool isNightlyBuild = false;
        private IExecutor<string, Process> processExecutor = null;

        private ShellCommand(ShellCommandBuilder shellCommandBuilder)
        {
            this.language = shellCommandBuilder.GetLanguage();
            this.processExecutor = new ProcessExecutor();
            // TODO check from environment variables if nightly build is set
        }

        public void Dispose()
        {
            process.Kill();
        }

        public async Task<Process> ExecuteCommandAsync()
        {
            String cmd = funcAppCmd;
            if(!isNightlyBuild)
            {
                cmd = dockerCmd;
            }
            // TODO fix the command building issues
            return await processExecutor.ExecuteAsync(cmd);
        }

        public sealed class ShellCommandBuilder
        {
            private Language language;
            // TODO
            // 1. create entity for Credentials for eventhub & confluent both
            // needed to be passed in env vars in docker & needed to set in-case
            // app needed to run directly on image
            //

            public ShellCommandBuilder() { }

            public ShellCommandBuilder SetLanguage(Language language)
            {
                this.language = language;
                return this;
            }
            public ShellCommand Build()
            {
                return new ShellCommand(this);
            }
            public Language GetLanguage()
            {
                return this.language;
            }
        }
    }
}
