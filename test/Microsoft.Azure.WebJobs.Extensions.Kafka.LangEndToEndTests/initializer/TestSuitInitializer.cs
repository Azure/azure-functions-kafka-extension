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
        //Intialize Helper vs Constants?
        private InitializeHelper initializeHelper = InitializeHelper.GetInstance();

        //Async
        //Why does this return ICommand?
        public void InitializeTestSuit(Language language)
        {
            /*CreateEventHub(language);
            ClearStorageQueue(language);
            StartupApplication(language);*/
            var startupApplication = StartupApplicationAsync(language);
            // var clearStorageQueueTask = ClearStorageQueueAsync(language);
            // var createEventHubTask = CreateEventHubAsync(language);
            //Task.WaitAll(startupApplication, clearStorageQueueTask, createEventHubTask);
            Task.WaitAll(startupApplication);
        }

        private void StartupApplication(Language language)
        {
            Command<Process> command = new ShellCommand.ShellCommandBuilder().SetLanguage(language).Build();
            IExecutor<Command<Process>, Process> executor = new ShellCommandExecutor();
            var process = executor.ExecuteAsync(command);
            /*
             * commenting for now for some issues TODO to fix the app issue
             * if(process != null && !process.HasExited)
            {
                return;
            }*/
            // TODO throw excpetion app startup failed
        }

        private async Task StartupApplicationAsync(Language language)
        {
            Command<Process> command = new ShellCommand.ShellCommandBuilder().SetLanguage(language).Build();
            IExecutor<Command<Process>, Process> executor = new ShellCommandExecutor();
            var process = await executor.ExecuteAsync(command);
            /*
             * commenting for now for some issues TODO to fix the app issue
             * if(process != null && !process.HasExited)
            {
                return;
            }*/
            // TODO throw excpetion app startup failed
        }

        private void ClearStorageQueue(Language language)
        {
            string singleEventStorageQueueName = initializeHelper.BuildStorageQueueName(QueueType.AzureStorageQueue, AppType.SINGLE_EVENT, language);
            string multiEventStorageQueueName = initializeHelper.BuildStorageQueueName(QueueType.AzureStorageQueue, AppType.BATCH_EVENT, language);
            ClearStorageQueue(singleEventStorageQueueName, multiEventStorageQueueName);
        }

        private async Task ClearStorageQueueAsync(Language language)
        {
            string singleEventStorageQueueName = initializeHelper.BuildStorageQueueName(QueueType.AzureStorageQueue, AppType.SINGLE_EVENT, language);
            string multiEventStorageQueueName = initializeHelper.BuildStorageQueueName(QueueType.AzureStorageQueue, AppType.BATCH_EVENT, language);
            await ClearStorageQueueAsync(singleEventStorageQueueName, multiEventStorageQueueName);
        }

        private void ClearStorageQueue(string singleEventStorageQueueName, string multiEventStorageQueueName)
        {
            IQueueManager<List<string>, List<string>> queueManager = AzureStorageQueueManager.GetInstance();
            //Async
            // var singleEventAzureStorageQueueClearTask = Task.Factory.StartNew(() => queueManager.clear(singleEventStorageQueueName));
            var singleEventAzureStorageQueueClearTask = queueManager.clearAsync(singleEventStorageQueueName);
            // var multiEventAzureStorageQueueClearTask = Task.Factory.StartNew(() => queueManager.clear(multiEventStorageQueueName));
            var multiEventAzureStorageQueueClearTask = queueManager.clearAsync(multiEventStorageQueueName);
            Task.WaitAll(singleEventAzureStorageQueueClearTask, multiEventAzureStorageQueueClearTask);
        }

        private async Task ClearStorageQueueAsync(string singleEventStorageQueueName, string multiEventStorageQueueName)
        {
            IQueueManager<List<string>, List<string>> queueManager = AzureStorageQueueManager.GetInstance();
            //Async
            // var singleEventAzureStorageQueueClearTask = Task.Factory.StartNew(() => queueManager.clear(singleEventStorageQueueName));
            var singleEventAzureStorageQueueClearTask = queueManager.clearAsync(singleEventStorageQueueName);
            // var multiEventAzureStorageQueueClearTask = Task.Factory.StartNew(() => queueManager.clear(multiEventStorageQueueName));
            var multiEventAzureStorageQueueClearTask = queueManager.clearAsync(multiEventStorageQueueName);
            await Task.WhenAll(singleEventAzureStorageQueueClearTask, multiEventAzureStorageQueueClearTask);
        }

        private void CreateEventHub(Language language)
        {
            string eventHubSingleName = initializeHelper.BuildCloudBrokerName(queue.QueueType.EventHub,
                    apps.type.AppType.SINGLE_EVENT, language);
            string eventHubMultiName = initializeHelper.BuildCloudBrokerName(queue.QueueType.EventHub,
                apps.type.AppType.BATCH_EVENT, language);

            BuildEventHub(eventHubSingleName, eventHubMultiName);
        }

        private async Task CreateEventHubAsync(Language language)
        {
            string eventHubSingleName = initializeHelper.BuildCloudBrokerName(queue.QueueType.EventHub,
                    apps.type.AppType.SINGLE_EVENT, language);
            string eventHubMultiName = initializeHelper.BuildCloudBrokerName(queue.QueueType.EventHub,
                apps.type.AppType.BATCH_EVENT, language);

            await BuildEventHubAsync(eventHubSingleName, eventHubMultiName);
        }

        private void BuildEventHub(string eventhubNameSingleEvent, string eventhubNameMultiEvent)
        {
            // TODO move this into Command from here
            IQueueManager<string, string> queueManager = EventHubQueueManager.GetInstance();
            // var singleEventEventHubTask = Task.Factory.StartNew(() => queueManager.create(eventhubNameSingleEvent));
            var singleEventEventHubTask = queueManager.createAsync(eventhubNameSingleEvent);
            // var multiEventEventHubTask = Task.Factory.StartNew(() => queueManager.create(eventhubNameMultiEvent));
            var multiEventEventHubTask = queueManager.createAsync(eventhubNameMultiEvent);
            Task.WaitAll(singleEventEventHubTask, multiEventEventHubTask);
        }

        private async Task BuildEventHubAsync(string eventhubNameSingleEvent, string eventhubNameMultiEvent) 
        {

            IQueueManager<string, string> queueManager = EventHubQueueManager.GetInstance();
            // var singleEventEventHubTask = Task.Factory.StartNew(() => queueManager.create(eventhubNameSingleEvent));
            var singleEventEventHubTask = queueManager.createAsync(eventhubNameSingleEvent);
            // var multiEventEventHubTask = Task.Factory.StartNew(() => queueManager.create(eventhubNameMultiEvent));
            var multiEventEventHubTask = queueManager.createAsync(eventhubNameMultiEvent);
            await Task.WhenAll(singleEventEventHubTask, multiEventEventHubTask);

        }
    }
}
