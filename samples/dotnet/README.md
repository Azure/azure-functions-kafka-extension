# .NET Sample Function

## Prerequiste

* [Visual Studio 2019](https://azure.microsoft.com/downloads/) and the [lateset Azure Functions tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-develop-vs#check-your-tools-version)

For more details refer to [uickstart: Create your first function in Azure using Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio#prerequisites)

This sample will work not only the Windows and Visual Studio. It works on Mac and Linux as well. However, this sample is tested on that enviornment. You can find several way to create a .NET function.

* [Visual Studio Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code?pivots=programming-language-csharp)
* [Visual Studio](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio)
* [Command line](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli?tabs=bash%2Cbrowser&pivots=programming-language-csharp)

## Configuration

A sample function is provided in folder samples/dotnet/KafkaFunctionSample. It depends on the Kafka installed locally (localhost:9092), as described in previous section. Add a local.settings.json files that looks like this:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "None",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "LocalBroker": "localhost:9092"
  }
}
```

If you have problems connecting to localhost:9092 try to add `broker    127.0.0.1` to your host file and use instead of localhost.

# Quick Start

You can refer [Quick Start](https://github.com/Azure/azure-functions-kafka-extension#quickstart) on the top page

# Test

Restore, Build, and Debug `KafkaFunctionSample`.

POST request `http://localhost:7071/api/ProduceStringTopic` 
with JSON Body and ContentType = `application/json`.

```json
{"hello":"world"}
```
