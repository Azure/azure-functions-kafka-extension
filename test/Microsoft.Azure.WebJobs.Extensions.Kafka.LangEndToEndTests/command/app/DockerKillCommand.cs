using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
    /* Shell Command responsible for kill the running the docker container 
     * containing function app images for particular language.
    */
    public class DockerKillCommand : ShellCommand
	{
		public DockerKillCommand(BrokerType brokerType, Language language)
		{
            cmd = buildDockerKillCmd(brokerType, language);
        }
        private string buildDockerKillCmd(BrokerType brokerType, Language language)
        {
            //Starting the list with docker rm
            List<string> cmdList = new List<string>() { Constants.DOCKER_KILL };

            //Adding the image name to kill
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

            return string.Join(Constants.SPACE_CHAR, cmdList);
        }
    }
}
