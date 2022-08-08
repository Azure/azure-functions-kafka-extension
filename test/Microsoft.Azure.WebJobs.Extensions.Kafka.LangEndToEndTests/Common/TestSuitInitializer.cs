// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

/* Responsible for all initilisation before actual test startup -
* Creation of Azure resources - Eventhubs and Storage Queues
* Function App startup
*/
public class TestSuitInitializer
{
	private readonly ILogger _logger = TestLogger.GetTestLogger();

	public void InitializeTestSuit(Language language, BrokerType brokerType)
	{
		CreateAzureResources(language, brokerType);
		Task.WaitAll(StartupApplicationAsync(language, brokerType));
	}

	private void CreateAzureResources(Language language, BrokerType brokerType)
	{
		var taskList = new List<Task>();

		if (BrokerType.EVENTHUB == brokerType)
		{
			taskList.Add(CreateEventHubAsync(language));
		}

		taskList.Add(ClearStorageQueueAsync(language, brokerType));

		Task.WaitAll(taskList.ToArray());
	}

	private async Task StartupApplicationAsync(Language language, BrokerType brokerType)
	{
		IExecutableCommand<Process> command =
			ShellCommandFactory.CreateShellCommand(ShellCommandType.DOCKER_RUN, brokerType, language);
		IExecutor<IExecutableCommand<Process>, Process> executor = new ShellCommandExecutor();
		ProcessLifecycleManager.GetInstance().AddProcess(await executor.ExecuteAsync(command));
	}

	private async Task ClearStorageQueueAsync(Language language, BrokerType brokerType)
	{
		var singleEventStorageQueueName = Utils.BuildStorageQueueName(brokerType,
			AppType.SINGLE_EVENT, language);
		var multiEventStorageQueueName = Utils.BuildStorageQueueName(brokerType,
			AppType.BATCH_EVENT, language);

		await ClearStorageQueueAsync(singleEventStorageQueueName, multiEventStorageQueueName);
	}

	private async Task ClearStorageQueueAsync(string singleEventStorageQueueName, string multiEventStorageQueueName)
	{
		IExecutableCommand<QueueResponse> singleCommand = new QueueCommand(QueueType.AzureStorageQueue,
			QueueOperation.CLEAR, singleEventStorageQueueName);
		IExecutableCommand<QueueResponse> multiCommand = new QueueCommand(QueueType.AzureStorageQueue,
			QueueOperation.CLEAR, multiEventStorageQueueName);

		await Task.WhenAll(singleCommand.ExecuteCommandAsync(), multiCommand.ExecuteCommandAsync());
	}

	private async Task CreateEventHubAsync(Language language)
	{
		var eventHubSingleName = Utils.BuildCloudBrokerName(QueueType.EventHub,
			AppType.SINGLE_EVENT, language);
		var eventHubMultiName = Utils.BuildCloudBrokerName(QueueType.EventHub,
			AppType.BATCH_EVENT, language);

		_logger.LogInformation($"Create Eventhub {eventHubSingleName} {eventHubMultiName}");

		await BuildEventHubAsync(eventHubSingleName, eventHubMultiName);
	}

	private async Task BuildEventHubAsync(string eventhubNameSingleEvent, string eventhubNameMultiEvent)
	{
		IExecutableCommand<QueueResponse> singleCommand = new QueueCommand(QueueType.EventHub,
			QueueOperation.CREATE, eventhubNameSingleEvent);
		IExecutableCommand<QueueResponse> multiCommand = new QueueCommand(QueueType.EventHub,
			QueueOperation.CREATE, eventhubNameMultiEvent);

		await Task.WhenAll(singleCommand.ExecuteCommandAsync(), multiCommand.ExecuteCommandAsync());
	}
}