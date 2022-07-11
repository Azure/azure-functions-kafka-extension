using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app
{
	public class DockerRunCommand : ShellCommand
	{
		public DockerRunCommand(BrokerType brokerType, Language language) 
		{
            cmd = buildDockerStartCmd(brokerType, language);
        }

        private string buildDockerStartCmd(BrokerType brokerType, Language language)
        {
            //Starts the list with docker run and port specific to language
            List<string> cmdList = new List<string>() { Constants.DOCKER_RUN, Constants.DOCKER_PORT_FLAG, $"{Constants.BrokerLanguagePortMapping[new Tuple<BrokerType, Language>(brokerType, language)]}{Constants.COLON_7071}" };

            //Adding Provider Specific variables 
            if (BrokerType.CONFLUENT == brokerType)
            {
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.CONFLUENT_USERNAME_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.CONFLUENT_PASSWORD_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.CONFLUENT_BROKERLIST_VAR);
            }
            else if (BrokerType.EVENTHUB == brokerType)
            {
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.EVENTHUB_CONSTRING_VAR);
                cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
                cmdList.Add(Constants.EVENTHUB_BROKERLIST_VAR);
            }

            //Adding env variable for the Storage Account
            cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
            cmdList.Add(Constants.AZURE_WEBJOBS_STORAGE);

            //Creating container with the same name as the image
            cmdList.Add(Constants.DOCKER_NAME_FLAG);
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

            //Adding the docker image name
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

            return string.Join(Constants.SPACE_CHAR, cmdList);
        }
    }
}
