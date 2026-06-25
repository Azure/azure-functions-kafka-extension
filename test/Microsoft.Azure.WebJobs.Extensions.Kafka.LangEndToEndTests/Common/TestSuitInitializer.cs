// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

/* Responsible for all initilisation before actual test startup -
* Creation of Azure resources - Eventhubs and Storage Queues
* Function App startup
*/
public class TestSuitInitializer
{
	private const int FunctionAppStartupAttempts = 60;
	private static readonly TimeSpan FunctionAppStartupDelay = TimeSpan.FromSeconds(5);
	private static readonly HttpClient HttpClient = new();
	private readonly ILogger _logger = TestLogger.GetTestLogger();

	public async Task InitializeTestSuitAsync(Language language, BrokerType brokerType)
	{
		await CreateAzureResourcesAsync(language, brokerType);
		await StartupApplicationAsync(language, brokerType);
	}

	private async Task CreateAzureResourcesAsync(Language language, BrokerType brokerType)
	{
		var taskList = new List<Task>();

		if (BrokerType.EVENTHUB == brokerType)
		{
			taskList.Add(CreateEventHubAsync(language));
		}

		taskList.Add(ClearStorageQueueAsync(language, brokerType));

		await Task.WhenAll(taskList);
	}

	private async Task StartupApplicationAsync(Language language, BrokerType brokerType)
	{
		var buildConfiguration = TestFunctionAppBuildConfiguration.FromEnvironment();
		IExecutableCommand<Process> command =
			ShellCommandFactory.CreateShellCommand(ShellCommandType.DOCKER_RUN, brokerType, language, buildConfiguration);
		IExecutor<IExecutableCommand<Process>, Process> executor = new ShellCommandExecutor();
		var process = await executor.ExecuteAsync(command);
		ProcessLifecycleManager.GetInstance().AddProcess(process);
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"Function App container failed to start. Docker command exited with code {process.ExitCode}.");
		}

		await WaitForFunctionAppStartupAsync(language, brokerType);
	}

	private async Task WaitForFunctionAppStartupAsync(Language language, BrokerType brokerType)
	{
		var port = Constants.BrokerLanguagePortMapping[new Tuple<BrokerType, Language>(brokerType, language)];
		var statusUri = new Uri($"http://localhost:{port}/admin/host/status");
		Exception lastException = null;

		for (var attempt = 1; attempt <= FunctionAppStartupAttempts; attempt++)
		{
			try
			{
				var response = await HttpClient.GetAsync(statusUri);
				if (response.IsSuccessStatusCode)
				{
					var content = await response.Content.ReadAsStringAsync();
					if (content.Contains("\"state\":\"Running\"", StringComparison.OrdinalIgnoreCase))
					{
						_logger.LogInformation($"Function App for {language} {brokerType} is ready at {statusUri}.");
						return;
					}
				}
			}
			catch (Exception ex)
			{
				lastException = ex;
			}

			await Task.Delay(FunctionAppStartupDelay);
		}

		throw new TimeoutException($"Function App for {language} {brokerType} did not become ready at {statusUri}.", lastException);
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