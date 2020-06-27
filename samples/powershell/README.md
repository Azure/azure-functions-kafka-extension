# Developers guide for Kafka Functions for PowerShell
Explain how to configure and run the sample

# Prerequisite
Powershell works not only windows but OSX and Linux and .NET Core 2.2+
However, this tutorial focus on Windows with Powershell usage.

* [.NET Core SDK 3.1](https://dotnet.microsoft.com/download)
* [Azure Functions Core Tools v3](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=windows%2Ccsharp%2Cbash)

# Installation

## Install Kafka extensions

```powershell
PS1 > func extensions install
```

## Configuration

Copy the `local.settings.json.example` to `local.settings.json.` 
Change confguration for your Kafka Cluster.

| Name | Description | NOTE |
| BrokerList | Kafka Broker List | e.g. changeme.eastus.azure.confluent.cloud:9092 |
| ConfluentCloudUsername | Username of Confluent Cloud | - |
| ConfluentCloudPassword | Password of Confluent Cloud | - |

For more configuration, you can refer to the samples [README.md](../README.md)

# Run Functions 

## Startup Kafka cluster

This sample is written for [Confluent cloud](https://docs.confluent.io/current/quickstart/cloud-quickstart/index.html#cloud-quickstart). The Confluent Cloud is a managed service of Kafka clusters. 
If you want to run it on your PC, You can run a Kafka cluster using docker-compose.
For more details, please refer to the following link.

* [Testing](https://github.com/Azure/azure-functions-kafka-extension#testing)

## Start Azure Functions

```powershell
PS1 > func start
```

## Send Events

Send Kafka events from a producer, and you can use [ccloud](https://docs.confluent.io/current/cloud/cli/index.html) command for the confluent cloud.

```bash
$ ccloud login
$ ccloud kafka topic produce message
```

For more details, Go to [ccloud](https://docs.confluent.io/current/cloud/cli/command-reference/ccloud.html).

If you want to send an event to the local Kafka cluster, you can use
[kafakacat](https://docs.confluent.io/current/app-development/kafkacat-usage.html) instead.

```bash
$ apt-get update && apt-get install kafkacat
$ kafkacat -b broker:29092 -t users -P
```

# Resource
* [Confluent cloud Quick Start](https://docs.confluent.io/current/quickstart/cloud-quickstart/index.html#cloud-quickstart)
* [Quickstart: Create a function in Azure using Visual Studio Code](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-function-vs-code?pivots=programming-language-powershell)