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
			Constants.DOCKER_RUN, Constants.DOCKER_DETACH_FLAG, Constants.DOCKER_PORT_FLAG,
			$"{Constants.BrokerLanguagePortMapping[new Tuple<BrokerType, Language>(brokerType, language)]}{Constants.COLON_7071}",
			Constants.DOCKER_NETWORK_FLAG, Constants.LOCAL_DOCKER_NETWORK
		};

		cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
		cmdList.Add(BuildEnvironmentVariableArgument(Constants.FUNCTIONS_WORKER_RUNTIME,
			Constants.LanguageRuntimeMapping[language]));

		//Adding Provider Specific variables 
		if (BrokerType.CONFLUENT == brokerType)
		{
			cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
			cmdList.Add(BuildEnvironmentVariableArgument(Constants.CONFLUENT_BROKERLIST_VAR,
				Environment.GetEnvironmentVariable(Constants.CONFLUENT_BROKERLIST_VAR) ?? Constants.LOCAL_KAFKA_BROKER_LIST));
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
		cmdList.Add(BuildEnvironmentVariableArgument(Constants.AZURE_WEBJOBS_STORAGE,
			StorageConnectionConfiguration.GetFunctionAppConnectionString()));

		if (_buildConfiguration?.HasExtensionSource == true)
		{
			cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
			cmdList.Add(BuildEnvironmentVariableArgument(Constants.EXTENSION_SOURCE_ENV_VAR, _buildConfiguration.ExtensionSource));
		}

		if (_buildConfiguration?.HasExtensionBundleVersion == true)
		{
			cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
			cmdList.Add(BuildEnvironmentVariableArgument(Constants.EXTENSION_BUNDLE_VERSION_ENV_VAR,
				_buildConfiguration.ExtensionBundleVersion));

			if (_buildConfiguration.UseExtensionBundle)
			{
				cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
				cmdList.Add(BuildEnvironmentVariableArgument(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR,
					GetExtensionBundleVersionRange(_buildConfiguration.ExtensionBundleVersion)));
			}
		}

		var extensionBundleSourceUri = Environment.GetEnvironmentVariable(Constants.FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI);
		if (!string.IsNullOrWhiteSpace(extensionBundleSourceUri))
		{
			cmdList.Add(Constants.DOCKER_ENVVAR_FLAG);
			cmdList.Add(BuildEnvironmentVariableArgument(Constants.FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI, extensionBundleSourceUri));
		}

		//Creating container with the same name as the image
		cmdList.Add(Constants.DOCKER_NAME_FLAG);
		cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

		//Adding the docker image name
		cmdList.Add(Constants.BrokerLanguageImageMapping[new Tuple<BrokerType, Language>(brokerType, language)]);

		return string.Join(Constants.STRINGLITERAL_SPACE_CHAR, cmdList);
	}

	private static string BuildEnvironmentVariableArgument(string name, string value)
	{
		return $"\"{name}={value}\"";
	}

	private static string GetExtensionBundleVersionRange(string extensionBundleVersion)
	{
		if (Environment.GetEnvironmentVariable(Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR) is { } configuredRange
			&& !string.IsNullOrWhiteSpace(configuredRange))
		{
			return configuredRange;
		}

		if (!Version.TryParse(extensionBundleVersion, out var version) || version.Major < 0 || version.Minor < 0 || version.Build < 0)
		{
			throw new ArgumentException($"Invalid extension bundle version '{extensionBundleVersion}'. Use a semantic version like 4.37.0 or set {Constants.EXTENSION_BUNDLE_VERSION_RANGE_ENV_VAR}.");
		}

		return $"[{version.Major}.{version.Minor}.{version.Build},{version.Major + 1}.0.0)";
	}
}
