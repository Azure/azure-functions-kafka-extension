using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.cleanup
{
	public class TestSuiteCleaner
	{
		public async Task CleanupTestSuiteAsync(Language language, BrokerType brokerType) 
		{
			ProcessManager.GetInstance().Dispose();
			await CleanupAzureResourcesAsync(language, brokerType);
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
