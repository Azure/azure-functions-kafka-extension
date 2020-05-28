# Samples Overview

This repository contains a few samples to help you get started quickly with the Kafka extension.

## End to end example using Confluent Cloud
Please find an end to end sample using [Confluent Cloud](https://docs.microsoft.com/en-us/samples/azure/azure-functions-kafka-extension-sample-confluent/azure-functions-kafka-extension-sample-using-confluent-cloud/)

## Getting started with Kafka locally

To test the sample applications, you need to have access to a Kafka instance. Here are some ways you can get access to one.

### Visual Studio Core Remote - Containers
There are several DevContainer samples [here](https://github.com/microsoft/vscode-dev-containers). If you start the [Visual Studio Code](https://code.visualstudio.com/) on the target sample directory, it will automatically start a development environment on a Docker container with a local Kafka cluster. It is the easiest option for starting a Kafka cluster. [Developing inside a Container](https://code.visualstudio.com/docs/remote/containers)

### Confluent Docker Compose

We provide the Confluent Docker Compose sample to get started with a local Kafka and data generator.
Follow the guide at https://docs.confluent.io/current/quickstart/ce-docker-quickstart.html#cp-quick-start-docker.

Make sure you complete the steps at least until the topics page views, users, and pageviews_female are created (including data generators). The included .NET sample function contains a consumer for each of those three topics.

### Confluent Cloud

[Confluent Cloud](https://www.confluent.io/confluent-cloud/?_ga=2.64661359.1523477604.1592516088-840657119.1591307401) is a fully managed, cloud-native event stream platform powered by Apache Kafka. The samples include Confluence Cloud samples to understand real-world configuration.

## Language Support

Azure Functions Kafka Extension support several languages with the following samples. For more details and getting started, please refer to the links below.

| Language | Description | Link | DevContainer |
| -------- | ----------- | ---- | ------------ |
| C# | C# precompiled sample with Visual Studio | [Readme](dotnet/README.md)| No |
| Java | Java 8 sample | [Readme](java/README.md) | Yes |
| JavaScript | Node 12 sample | [Readme](javascript/README.md)| Yes |
| PowerShell | PowerShell 6 Sample | [Readme](powershell/README.md)| No |
| Python | Python 3.8 sample | [Readme](python/README.md)| Yes |
| TypeScript | TypeScript sample (Node 12) | [Readme](typescript/kafka-trigger/README.md)| Yes |

## Custom Container

Custom containers enable us to deploy a custom container to the Function App. We can use any other languages; however, as an example, we provide a java sample to explain how to develop it.

| Language | Description | Link | DevContainer |
| -------- | ----------- | ---- | ------------ |
| Java | Custom container sample | [Readme](container/kafka-function/README.md)| No |

## Notes

Kafka extension supports several languages, however, it uses the same Azure Functions host. For this reason, there is a common configuration for each language. Please find below some common notes with applying to all the languages.

### function.json

You can find all Kafka related configuration on the `function.json.` In the case of Java, you specify it as an annotation. However, the maven plugin generates the `function.json.` If your function doesn't work well, please check your code and `function.json` at first.

_function.json_

```json
{
  "scriptFile" : "../kafka-function-1.0-SNAPSHOT.jar",
  "entryPoint" : "com.contoso.kafka.TriggerFunction.runMany",
  "bindings" : [ {
    "type" : "kafkaTrigger",
    "direction" : "in",
    "name" : "kafkaEvents",
    "password" : "%ConfluentCloudPassword%",
    "protocol" : "SASLSSL",
    "dataType" : "string",
    "topic" : "message",
    "authenticationMode" : "PLAIN",
    "consumerGroup" : "$Default",
    "cardinality" : "MANY",
    "username" : "%ConfluentCloudUsername%",
    "brokerList" : "%BrokerList%"
  } ]
}
```

### local.settings.json

It is the configuration of a local function runtime. If you deploy the target application on Azure with a `local.settings.json,` you will require the same settings on the Function App [App settings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings#settings). 

For more details, refer to [Local settings file](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=macos%2Ccsharp%2Cbash#local-settings-file).

```json
{
    "IsEncrypted": false,
    "Values": {
        "BrokerList": "pkc-epwny.eastus.azure.confluent.cloud:9092",
        "ConfluentCloudUsername": "WHYYKOT6JMPYGJJV",
        "ConfluentCloudPassword": "KUtytGe72JzhCKeA7cZ6dJj6oRE4I5NyOjMxj157tQaiJoVy67UD0sAqJM9e4QAZ",
        "FUNCTIONS_WORKER_RUNTIME": "python",
        "AzureWebJobsStorage": ""
    }
}
```

### Extension Bundle and install Kafka extension

Currently, in Azure Functions - most triggers and bindings are ordinarily obtained using the extension bundle. However, currently, the Kafka extension is not part of the extension bundle (will be added in the future). Meanwhile, you will have to install the Kafka extension manually.

For installing Kafka extension manually:

#### Create extensions.csproj file

Azure Functions extension is written in C#. We need to install with .NET Core capability. `csproj` file is a project file for .NET. For more information refer to [Understanding the project file](https://docs.microsoft.com/en-us/aspnet/web-forms/overview/deployment/web-deployment-in-the-enterprise/understanding-the-project-file)

_extensions.csproj_

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  <WarningsAsErrors></WarningsAsErrors>
  <DefaultItemExcludes>**</DefaultItemExcludes>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.WebJobs.Extensions.Kafka" Version="3.0.0" />
    <PackageReference Include="Microsoft.Azure.WebJobs.Script.ExtensionsMetadataGenerator" Version="1.1.7" />
  </ItemGroup>
  <ItemGroup>
    <None Update="confluent_cloud_cacert.pem">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

#### Install the Kafka extension

You can use this command. It will refer to the `extensions.csproj` and install the related extension.

```bash
$ func extensions install
```


In the case of Java, they need to specify the extension name.

```bash
$ func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version ${EXTENSION_VERSION}
```

#### Check if the Kafka extension installed properly

You can go `bin/runtimes/` if you find librdkafka native libraries, the installation is succeeded.

### librdkafka library

Kafka extensions use libkafka native libraries. That is included in the Kafka Extensions NuGet package. However, for the Linux and OSX environment, you need to specify `LD_LIBRARY_PATH` for the Azure Functions runtime refer to the native library.

```bash
$ export LD_LIBRARY_PATH=/workspace/bin/runtimes/linux-x64/native
```

For the devcontainer, you will find the configuration on the `devcontainer.json.` If you deploy your app on the Linux Premium Functions, you need to configure App settings with `LD_LIBRARY_PATH.` For more details, refer to [Linux Premium plan configuration](https://github.com/Azure/azure-functions-kafka-extension#linux-premium-plan-configuration)

### Confluent Cloud Configuration

You can find the configuration for the Confluent Cloud for C# in 
[Connecting to Confluent Cloud in Azure](https://github.com/Azure/azure-functions-kafka-extension#connecting-to-confluent-cloud-in-azure).


### Install binding library (Java/Python)
Java and Python have a binding library. Currently, it resides in this repository. In the near feature, it will move to the official repo. So you don't need to install manually. 

However, currently, we need to install it manually. Please follow the instruction for each `README.md.` 

### Batch (Cardinality)

For the KafkaTrigger and non-C# implementation, if we want to execute Kafka trigger with batch, you can configure `cardinality` and `dataType`. For more details, refer to [Language support configuration](https://github.com/Azure/azure-functions-kafka-extension#language-support-configuration)

If you have problems connecting to localhost:9092 try to add `broker    127.0.0.1` to your host file and use instead of localhost.

### Visual Studio Code Remote Containers

The sample provides a devcontainer profile. Open the folder in VsCode and perform the action `Remote-Containers: Reopen in Container`. The action will reopen VsCode inside a container,  together with the Confluent's Kafka starter sample. Then run the function inside the remote container using the following local.settings.json file:

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "node",
    "AzureWebJobsStorage": "{AzureWebJobsStorage}",
    "BrokerList":"broker:29092"
  }
}
```
