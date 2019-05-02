# Samples

This repository contains a few samples to help you get started quickly with the Kafka extension.

## Getting started with Kafka locally

In order to test the sample applications you need to have access to a Kafka instance. We recommend using the Confluent Docker Compose sample to get started with a local Kafka and data generator.
Follow the guide at https://docs.confluent.io/current/quickstart/ce-docker-quickstart.html#cp-quick-start-docker.

Make sure you complete the steps at least until the topics pageviews, users and pageviews_female are created (including data generators). The included .NET sample function contains a consumer for each of those 3 topics.

## Using the .NET Sample Function

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

[TODO: add information about .NET consumer sample]

[TODO: add information about .NET producer sample]

[TODO: add information about .NET Host sample]

## Python Consumer

A sample Python consumer function is provided in samples/python/KafkaTrigger. It depends on the Kafka installed locally (localhost:9092), as described in previous section.

### Using the Azure Functions Python Kakfa Trigger

1. Make sure you have access to a Kafka Cluster. Follow [these](https://medium.com/@tsuyoshiushio/local-kafka-cluster-on-kubernetes-on-your-pc-in-5-minutes-651a2ff4dcde) steps to set it up locally

2. Make sure you have [latest version](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local) of functions core tools.
[TODO: add information Java samples]
