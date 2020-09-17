# Custom Container with Kafka extension

You can use the Kafka extension with a custom container. If you don't know about the custom container, you can refer to [Create a function on Linux using a custom container](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-linux-custom-image?tabs=bash%2Cportal&pivots=programming-language-csharp).

This sample is for java. However, you can pick any languages. You can follow the step of the [Create a function on Linux using a custom container](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-linux-custom-image?tabs=bash%2Cportal&pivots=programming-language-csharp) with additional steps only for the Kafka extensions.

## Kafka extension additional steps

1. Install the Kafka extension
2. Set the LD_LIBRARY_PATH

For the 1. and 2., the step is included on the `Dockerfile.`

# Quick Start

## Build, Test, Publish the Docker container

Refer to the [Dockerfile](./Dockerfile). The `Dockerfile` execute these steps. 

**NOTE:** You need to use `azure-functions-java-library` 1.4.0+ and `azure-functions-maven-plugin` 1.6.0+.


* mvn clean package
* Install Kafka Extension
* LD_LIBRARY_PATH as an environment variables

### Build

```bash
$ docker build --tag {YOUR_DOCKERHUB_NAME}/azurefunctionsimage:v1.0.0 .
```

### Test

This sample is configured for [Connecting to Confluent Cloud in Azure](https://github.com/Azure/azure-functions-kafka-extension#connecting-to-confluent-cloud-in-azure). If you want to get simple configuration, refer to the [Java sample](../java/README.md) or [Connecting to Confluent Cloud in Azure](https://github.com/Azure/azure-functions-kafka-extension#connecting-to-confluent-cloud-in-azure)

If you want to run locally, you need to pass the credentials for the Confluent Cloud. In this sample, I use an Azure Storage Account. Please refer to [Create an Azure Storage account](https://docs.microsoft.com/en-us/azure/storage/common/storage-account-create?tabs=azure-portal).

You can see the log with sending the Kafka event. For the Confluence Cloud, you can use [ccloud](https://docs.confluent.io/current/cloud/cli/index.html) utility. Otherwise, you can use [kafkacat](https://docs.confluent.io/current/app-development/kafkacat-usage.html) to send the event.

```bash
$ docker run -p 8080:80 -it -e "BrokerList={YOUR_CONFLUENT_CLOUD_NAME}.eastus.azure.confluent.cloud:9092" -e ConfluentCloudUsername={YOUR_CONFLUENT_CLOUD_USERNAME} -e ConfluentCloudPassword={YOUR_CONFLUENT_CLOUD_PASSWORD} -e AzureWebJobsStorage="{YOUR_STORAGE_ACCOUNT_CONNECTION_STRING}" -e FUNCTIONS_WORKER_RUNTIME=java tsuyoshiushio/azurefunctionsimage:v1.0.0
```

### Push the container

```bash
$ docker login
$ docker push docker push tsuyoshiushio/azurefunctionsimage:v1.0.0
```

## Create and Configure Linux Premium Function App

Create a Linux based Premium Function. Follow the step of [Create supporting Azure resources for your function](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-linux-custom-image?tabs=bash%2Cportal&pivots=programming-language-java#create-supporting-azure-resources-for-your-function) and [Create and configure a function app on Azure with the image](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-function-linux-custom-image?tabs=bash%2Cportal&pivots=programming-language-java#create-and-configure-a-function-app-on-azure-with-the-image).

These steps create a Premium Function App and set the container image on the Function APp. 

## Configure the Kafka credentials

Configure the Kafka credentials to the function app.

```
$ az functionapp config appsettings set --name <app_name> --resource-group <source_group_name> --settings "BrokerList={YOUR_CONFLUENT_CLOUD_NAME}.eastus.azure.confluent.cloud:9092 ConfluentCloudUsername={YOUR_CONFLUENT_CLOUD_USERNAME} ConfluentCloudPassword={YOUR_CONFLUENT_CLOUD_PASSWORD}
```

**NOTE**: You don't need `LD_LIBRARY_PATH` for this scenario. It is already configured on the `Dockerfile`.