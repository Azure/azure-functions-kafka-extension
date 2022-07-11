using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.cleanup
{
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
			/*Command<Process> command = new ShellCommand.ShellCommandBuilder()
											.SetLanguage(language)
											.SetBrokerType(brokerType)
											.SetShellCommandType(ShellCommandType.DOCKER_KILL)
											.Build();*/
			Command<Process> command = ShellCommandFactory.CreateShellCommand(ShellCommandType.DOCKER_KILL, brokerType, language);
			IExecutor<Command<Process>, Process> executor = new ShellCommandExecutor();
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

			Command<QueueResponse> singleCommand = new QueueCommand(QueueType.EventHub,
									QueueOperation.DELETE, eventHubSingleName);
			Command<QueueResponse> multiCommand = new QueueCommand(QueueType.EventHub,
						QueueOperation.DELETE, eventHubMultiName);

			await Task.WhenAll(singleCommand.ExecuteCommandAsync(), multiCommand.ExecuteCommandAsync());
		}
	}
}
