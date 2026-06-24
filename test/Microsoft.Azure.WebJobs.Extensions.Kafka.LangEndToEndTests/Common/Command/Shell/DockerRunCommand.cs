// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

/* Shell Command responsible for creating running the docker container 
* containing function app images for particular language.
*/
public class DockerRunCommand : ShellCommand
{
	private readonly TestFunctionAppBuildConfiguration _buildConfiguration;

	public DockerRunCommand(BrokerType brokerType, Language language, TestFunctionAppBuildConfiguration buildConfiguration = null)
	{
		_buildConfiguration = buildConfiguration;
		cmd = BuildDockerStartCmd(brokerType, language);
	}

	private string BuildDockerStartCmd(BrokerType brokerType, Language language)
	{
		//Starts the list with docker run and port specific to language
		var cmdList = new List<string>
		{
			Constants.DOCKER_RUN, Constants.DOCKER_PORT_FLAG,
			$"{Constants.BrokerLanguagePortMapping[new Tuple<BrokerType, Language>(brokerType, language)]}{Constants.COLON_7071}"
		};

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

		if (_buildConfiguration?.HasExtensionSource == true)
		{
			cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
			cmdList.Add($"{Constants.EXTENSION_SOURCE_ENV_VAR}={_buildConfiguration.ExtensionSource}");
		}

		if (_buildConfiguration?.HasExtensionBundleVersion == true)
		{
			cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
			cmdList.Add($"{Constants.EXTENSION_BUNDLE_VERSION_ENV_VAR}={_buildConfiguration.ExtensionBundleVersion}");
		}

		//Creating container with the same name as the image
		cmdList.Add(Constants.DOCKER_NAME_FLAG);
		cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

		//Adding the docker image name
		cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

		return string.Join(Constants.STRINGLITERAL_SPACE_CHAR, cmdList);
	}
}