// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	/* Responsible for all cleanup after the test suite runs -
	 * Kills the running docker containers
	 * Kills all the processes created
	 * Cleans up the used Azure Resources
	*/
	public class TestSuiteCleaner
	{
		public async Task CleanupTestSuiteAsync(Language language, BrokerType brokerType)
		{
			//Kill all docker containers
			await KillFunctionDockersAsync(language, brokerType);
			ProcessLifecycleManager.GetInstance().Dispose();
			await CleanupAzureResourcesAsync(language, brokerType);
		}

		private async Task KillFunctionDockersAsync(Language language, BrokerType brokerType)
		{
			IInfraCommand<Process> command = ShellCommandFactory.CreateShellCommand(ShellCommandType.DOCKER_KILL, brokerType, language);
			IExecutor<IInfraCommand<Process>, Process> executor = new ShellCommandExecutor();
			await executor.ExecuteAsync(command);
		}

		private async Task CleanupAzureResourcesAsync(Language language, BrokerType brokerType)
		{
			var taskList = new List<Task>();
			if (BrokerType.EVENTHUB == brokerType)
			{
				taskList.Add(CleanupEventhubAsync(language));
			}
			await Task.WhenAll(taskList);
		}

		private async Task CleanupEventhubAsync(Language language)
		{
			string eventHubSingleName = Utils.BuildCloudBrokerName(QueueType.EventHub,
						AppType.SINGLE_EVENT, language);
			string eventHubMultiName = Utils.BuildCloudBrokerName(QueueType.EventHub,
						AppType.BATCH_EVENT, language);

			IInfraCommand<QueueResponse> singleCommand = new QueueCommand(QueueType.EventHub,
									QueueOperation.DELETE, eventHubSingleName);
			IInfraCommand<QueueResponse> multiCommand = new QueueCommand(QueueType.EventHub,
						QueueOperation.DELETE, eventHubMultiName);

			await Task.WhenAll(singleCommand.ExecuteCommandAsync(), multiCommand.ExecuteCommandAsync());
		}
	}
}
