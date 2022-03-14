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
    public class ShellCommand: Command<Process>
    {
        private Process process;
        
        private readonly Language language;
        private readonly AppType appType;
        private readonly BrokerType brokerType;
        
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
            //process.Kill();
        }

        public async Task<Process> ExecuteCommandAsync()
        {
            String cmd = Constants.FUNC_START;
            if(!isNightlyBuild)
            {
                //Build Docker Command
                cmd = buildDockerCmd();
                
            }

            // TODO fix the command building issues -- What issue?
            this.process = await processExecutor.ExecuteAsync(cmd);
            return process;
        }

        private string buildDockerCmd() 
        {
            List<string> cmdList = new List<string>() { Constants.DOCKER_RUN, Constants.DOCKER_PORT_FLAG, Constants.LanguagePortMapping[language], Constants.COLON_7071, Constants.DOCKER_ENVVAR_FLAG };
            
            if (BrokerType.CONFLUENT == brokerType)
            {
                cmdList.Add(Constants.CONFLUENT_USERNAME_VAR);
                cmdList.Add(Constants.CONFLUENT_PASSWORD_VAR);
                cmdList.Add(Constants.CONFLUENT_BROKERLIST_VAR);
            }
            else if (BrokerType.EVENTHUB == brokerType)
            {
                cmdList.Add(Constants.EVENTHUB_CONSTRING_VAR);
                cmdList.Add(Constants.EVENTHUB_BROKERLIST_VAR);
            }
            
            cmdList.Add(Constants.LanguageImageMapping[language]);
            return string.Join(Constants.SPACE_CHAR, cmdList);

        }

        public sealed class ShellCommandBuilder
        {
            private Language language;
            private AppType appType;
            private BrokerType brokerType;
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
            public ShellCommandBuilder SetAppType(AppType appType)
            {
                this.appType = appType;
                return this;
            }
            public ShellCommandBuilder SetBrokerType(BrokerType brokerType)
            {
                this.brokerType = brokerType;
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
