using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.app;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.CommandExecutor;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.helper;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.eventhub;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.operation;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.storageQueue;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;
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
            Command<Process> command = new ShellCommand.ShellCommandBuilder()
                                            .SetLanguage(language)
                                            .SetBrokerType(brokerType)
                                            .Build();
            IExecutor<Command<Process>, Process> executor = new ShellCommandExecutor();
            ProcessManager.GetInstance().AddProcess(await executor.ExecuteAsync(command));
            //processes.Add(process);
            /*
             * commenting for now for some issues TODO to fix the app issue
             * if(process != null && !process.HasExited)
            {
                return;
            }*/
            // TODO throw excpetion app startup failed
        }

        private async Task ClearStorageQueueAsync(Language language, BrokerType brokerType)
        {
            string singleEventStorageQueueName = Utils.BuildStorageQueueName( brokerType, 
                        AppType.SINGLE_EVENT, language);
            string multiEventStorageQueueName = Utils.BuildStorageQueueName( brokerType, 
                        AppType.BATCH_EVENT, language);
            
            await ClearStorageQueueAsync(singleEventStorageQueueName, multiEventStorageQueueName);
        }

        private async Task ClearStorageQueueAsync(string singleEventStorageQueueName, string multiEventStorageQueueName)
        {
            Command<QueueResponse> singleCommand = new QueueCommand(QueueType.AzureStorageQueue,
                        QueueOperation.CLEAR, singleEventStorageQueueName);
            Command<QueueResponse> multiCommand = new QueueCommand(QueueType.AzureStorageQueue,
                        QueueOperation.CLEAR, multiEventStorageQueueName);
            
            await Task.WhenAll(singleCommand.ExecuteCommandAsync(), multiCommand.ExecuteCommandAsync());
        }

        private async Task CreateEventHubAsync(Language language)
        {
            string eventHubSingleName = Utils.BuildCloudBrokerName(QueueType.EventHub,
                        AppType.SINGLE_EVENT, language);
            string eventHubMultiName = Utils.BuildCloudBrokerName(QueueType.EventHub,
                        AppType.BATCH_EVENT, language);
            Console.WriteLine($"Create Eventhub {eventHubSingleName} {eventHubMultiName}");

            await BuildEventHubAsync(eventHubSingleName, eventHubMultiName);
        }

        private async Task BuildEventHubAsync(string eventhubNameSingleEvent, string eventhubNameMultiEvent) 
        {
            Command<QueueResponse> singleCommand = new QueueCommand(QueueType.EventHub, 
                        QueueOperation.CREATE, eventhubNameSingleEvent);
            Command<QueueResponse> multiCommand = new QueueCommand(QueueType.EventHub, 
                        QueueOperation.CREATE, eventhubNameMultiEvent);
            
            await Task.WhenAll(singleCommand.ExecuteCommandAsync(), multiCommand.ExecuteCommandAsync());

        }
    }
}
