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
        private readonly BrokerType brokerType;
        private ShellCommandType shellCommandType = ShellCommandType.DOCKER_RUN;
        private bool isNightlyBuild = false;
        
        private IExecutor<string, Process> processExecutor = null;
        
        private ShellCommand(ShellCommandBuilder shellCommandBuilder)
        {
            this.language = shellCommandBuilder.GetLanguage();
            this.brokerType = shellCommandBuilder.GetBrokerType();
            this.shellCommandType = shellCommandBuilder.GetShellCommandType();
            this.processExecutor = new ProcessExecutor();
        }

        public async Task<Process> ExecuteCommandAsync()
        {
            string cmd = Constants.FUNC_START;
            if (ShellCommandType.DOCKER_RUN == shellCommandType)
            {
                cmd = buildDockerStartCmd();
            }
            else if (ShellCommandType.DOCKER_KILL == shellCommandType)
            {
                cmd = buildDockerKillCmd();
            }

            this.process = await processExecutor.ExecuteAsync(cmd);
            return process;
        }

        private string buildDockerKillCmd()
        {
            List<string> cmdList = new List<string>() { Constants.DOCKER_KILL };
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);
            return string.Join(Constants.SPACE_CHAR, cmdList);
        }

        private string buildDockerStartCmd() 
        {
            List<string> cmdList = new List<string>() { Constants.DOCKER_RUN, Constants.DOCKER_PORT_FLAG, $"{Constants.BrokerLanguagePortMapping[new Tuple<BrokerType, Language>(brokerType, language)] }{ Constants.COLON_7071 }", Constants.DOCKER_ENVVAR_FLAG };
            
            //Adding Env variables 
            if (BrokerType.CONFLUENT == brokerType)
            {
                cmdList.Add(Constants.CONFLUENT_USERNAME_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.CONFLUENT_PASSWORD_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.CONFLUENT_BROKERLIST_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.AZURE_WEBJOBS_STORAGE);
                //cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                //cmdList.Add($"FUNCTIONS_WORKER_RUNTIME='{Constants.LanguageRuntimeMapping[language]}'");
            }
            else if (BrokerType.EVENTHUB == brokerType)
            {
                cmdList.Add(Constants.EVENTHUB_CONSTRING_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.EVENTHUB_BROKERLIST_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.AZURE_WEBJOBS_STORAGE);
                //cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                //cmdList.Add(Constants.LanguageRuntimeMapping[language]);
            }

            //Creating container with the same name as the image
            cmdList.Add(Constants.DOCKER_NAME_FLAG);
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);
            
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);
            return string.Join(Constants.SPACE_CHAR, cmdList);

        }

        public void Dispose()
        {
            //process.Kill();
        }

        public sealed class ShellCommandBuilder
        {
            private Language language;
            private BrokerType brokerType;
            private ShellCommandType shellCommandType;
            
            public ShellCommandBuilder() { }

            public ShellCommandBuilder SetLanguage(Language language)
            {
                this.language = language;
                return this;
            }
            public ShellCommandBuilder SetBrokerType(BrokerType brokerType)
            {
                this.brokerType = brokerType;
                return this;
            }

            public ShellCommandBuilder SetShellCommandType(ShellCommandType shellCommandType) 
            {
                this.shellCommandType = shellCommandType;
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

            public BrokerType GetBrokerType()
            {
                return this.brokerType;
            }

            public ShellCommandType GetShellCommandType()
            {
                return this.shellCommandType;
            }
        }
    }
}
