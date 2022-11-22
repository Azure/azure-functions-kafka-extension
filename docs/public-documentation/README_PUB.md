# Public Documentation

# Overview

Azure Functions integrates with Kafka Brokers via [triggers and bindings](https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings?tabs=csharp). The Azure Functions Kafka extension allows you to send and receive messages using the Kafka Extension API with Functions.

| **Action** | **Type** |
| --- | --- |
| Run a function when a Kafka message comes through the queue | Trigger |
| Send Kafka messages | Output Binding |

# Install Extension

## C#

The extension NuGet package you install depends on the C# mode you&#39;re using in your function app:

### In-process

Functions execute in the same process as the Functions host. To learn more, see [Develop C# class library functions using Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-class-library).

Add the extension to your project by installing this [NuGet package](https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.RabbitMQ).

### Isolated process

Functions execute in an isolated C# worker process. To learn more, see [Guide for running C# Azure Functions in an isolated process](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide).

Add the extension to your project by installing this [NuGet package](https://www.nuget.org/packages/Microsoft.Azure.Functions.Worker.Extensions.Rabbitmq).

## Java/JavaScript/TypeScript/Powershell

Kafka extension is part of an [extension bundle](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-register#extension-bundles), which is specified in your host.json project file. When you create a project that targets version 3.x or later, you should already have this bundle installed. To learn more, see [extension bundle](https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-register#extension-bundles).

# Prerequisites

Before working with the Kafka extension, you must setup the managed Kafka Brokers either in [Confluent on Azure](https://azuremarketplace.microsoft.com/en-us/marketplace/apps/confluentinc.confluent-cloud-azure-prod) or [Confluent](https://www.confluent.io/confluent-cloud/) or [EventHub](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-about). To learn more about Kafka, please check their official documentation.

# Note

The Kafka bindings are only fully supported on [Premium](https://docs.microsoft.com/en-us/azure/azure-functions/functions-premium-plan) and [Dedicated App Service](https://docs.microsoft.com/en-us/azure/azure-functions/dedicated-plan) plans. Consumption plans aren&#39;t supported.
 Kafka bindings are only supported for Azure Functions version 3.x and later versions.

 # Next Steps

- Run a function when a Kafka message is created (Trigger)
- Send Kafka message from Azure Functions (Output Binding)