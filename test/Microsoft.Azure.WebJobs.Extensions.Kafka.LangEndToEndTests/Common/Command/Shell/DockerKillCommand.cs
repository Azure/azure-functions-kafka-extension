// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	/* Shell Command responsible for kill the running the docker container 
     * containing function app images for particular language.
    */
	public class DockerKillCommand : ShellCommand
	{
		public DockerKillCommand(BrokerType brokerType, Language language)
		{
            cmd = BuildDockerKillCmd(brokerType, language);
        }
        private string BuildDockerKillCmd(BrokerType brokerType, Language language)
        {
            //Starting the list with docker rm
            List<string> cmdList = new List<string>() { Constants.DOCKER_KILL };

            //Adding the image name to kill
            cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

            return string.Join(Constants.STRINGLITERAL_SPACE_CHAR, cmdList);
        }
    }
}
