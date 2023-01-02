# Distributed Tracing Sample

This folder contains sample for Distributed Tracing feature for **Dotnet in-proc** Kafka trigger functions. In this sample, we are using Kafka Trigger Functions with EventHub as the source. 

## Prereqs

Create a Application Insights resource from the [ Azure Portal/CLI.](https://learn.microsoft.com/en-us/azure/azure-monitor/app/create-new-resource?tabs=net). One can even use the existing App Insights resource created for Kafka Trigger Functions Monitoring.

## Configuration

Add a local.settings.json file to the project similar to this: 

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet",
        "BrokerList": "<YOUR_EVENTHUB_NAMESPACE_NAME>.servicebus.windows.net:9093",
        "EventHubConnectionString": "<YOUR_EVENTHUB_CONNECTIONSTRING>",
        "topic": "{YOUR_KAFKA_TOPIC_NAME}"
    }
}
```

Get the connection string for existing/new Application Insights resource from the [Azure portal](https://learn.microsoft.com/en-us/azure/azure-monitor/app/sdk-connection-string?tabs=net#get-started). 

Put this connection string value in the Startup.cs file in this folder as below: 

```cs
    .AddAzureMonitorTraceExporter(o =>
    {
        o.ConnectionString = "<APP_INSIGHTS_CONNECTION-STRING>";
    })
```
**Note:** We do not recommend to hard code connection string in the code for production environment. 

## Test

Produce Kafka Events with trace parent header in Kafka Headers. 

Run the Kafka Trigger Function using ```func start``` command. 
Copy the Activity Id Printed on the Console. Open the Transactions Search in Application Insights Page. Search the copied activity Id in Transactions Search.

Here, the user would be able to see the events processed with a "Operation-Id" as below:

### Single Kafka Event Trigger
![alt text](/images/SingleTrigger.png)

### Batch Kafka Event Trigger
![alt text](/images/BatchTrigger.png)