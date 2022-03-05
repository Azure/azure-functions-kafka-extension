using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.helper;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.initializer
{
    // build the eventhub name
    // build the azure storage queue name
    // InitializeTestSuit
    // orchestration of
    // InitializeInfra -- Azure
    // Initilize Kafka broker
    // start app
    public class TestSuitInitializer
    {
        private InitializeHelper initializeHelper = InitializeHelper.GetInstance();

        public ICommand InitializeTestSuit(Language language)
        {
            createEventHub(language);
            clearStorageQueue(language);
            startupApplication(language);
            return null;
        }

        private void startupApplication(Language language)
        {
            Command<Process> command = new ShellCommand.ShellCommandBuilder().SetLanguage(language).Build();
            IExecutor<Command<Process>, Process> executor = new ShellCommandExecutor();
            Process process = executor.Execute(command);
            /*
             * commenting for now for some issues TODO to fix the app issue
             * if(process != null && !process.HasExited)
            {
                return;
            }*/
            // TODO throw excpetion app startup failed
        }

        private void clearStorageQueue(Language language)
        {
            string singleEventStorageQueueName = initializeHelper.BuildStorageQueueName(QueueType.AzureStorageQueue, AppType.SINGLE_EVENT, language);
            string multiEventStorageQueueName = initializeHelper.BuildStorageQueueName(QueueType.AzureStorageQueue, AppType.BATCH_EVENT, language);
            clearStorageQueue(singleEventStorageQueueName, multiEventStorageQueueName);
        }

        private void clearStorageQueue(string singleEventStorageQueueName, string multiEventStorageQueueName)
        {
            IQueueManager<List<string>, List<string>> queueManager = AzureStorageQueueManager.GetInstance();
            var singleEventAzureStorageQueueClearTask = Task.Factory.StartNew(() => queueManager.clear(singleEventStorageQueueName));
            var multiEventAzureStorageQueueClearTask = Task.Factory.StartNew(() => queueManager.clear(multiEventStorageQueueName));
            Task.WaitAll(singleEventAzureStorageQueueClearTask, multiEventAzureStorageQueueClearTask);
        }

        private void createEventHub(Language language)
        {
            string eventHubSingleName = initializeHelper.BuildCloudBrokerName(queue.QueueType.EventHub,
                    apps.type.AppType.SINGLE_EVENT, language);
            string eventHubMultiName = initializeHelper.BuildCloudBrokerName(queue.QueueType.EventHub,
                apps.type.AppType.BATCH_EVENT, language);

            buildEventHub(eventHubSingleName, eventHubMultiName);
        }

        private void buildEventHub(string eventhubNameSingleEvent, string eventhubNameMultiEvent)
        {
            // TODO move this into Command from here
            IQueueManager<string, string> queueManager = EventHubQueueManager.GetInstance();
            var singleEventEventHubTask = Task.Factory.StartNew(() => queueManager.create(eventhubNameSingleEvent));
            var multiEventEventHubTask = Task.Factory.StartNew(() => queueManager.create(eventhubNameMultiEvent));
            Task.WaitAll(singleEventEventHubTask, multiEventEventHubTask);
        }

    }
}
