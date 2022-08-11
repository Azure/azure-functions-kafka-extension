// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Static Factory class to create supported Shell Commands
public static class ShellCommandFactory
{
	public static ShellCommand CreateShellCommand(ShellCommandType shellCommandType, BrokerType brokerType,
		Language language)
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