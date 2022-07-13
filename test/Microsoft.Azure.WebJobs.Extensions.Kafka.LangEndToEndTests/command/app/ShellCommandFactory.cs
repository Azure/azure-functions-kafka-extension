using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
    /* Static Factory class to create supported Shell Commands
    */
    public static class ShellCommandFactory
    {
        public static ShellCommand CreateShellCommand(ShellCommandType shellCommandType, BrokerType brokerType, Language language)
        {
            switch (shellCommandType)
            {
                case ShellCommandType.DOCKER_RUN:
                    return new DockerRunCommand(brokerType, language);
                case ShellCommandType.DOCKER_KILL:
                    return new DockerKillCommand(brokerType, language);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
