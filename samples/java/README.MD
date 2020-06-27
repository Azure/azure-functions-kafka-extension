# Developers guid for Kafka Functions for Java

Explain how to compile the Java bindings and run the sample. 

## Prerequiste

If you want to run the sample on Windows, OSX, or Linux, you need to refer to the [Readme](https://github.com/Azure/azure-functions-kafka-extension/tree/master/binding-library/java). If you want to update [Kafka binding library for java](https://github.com/Azure/azure-functions-kafka-extension/tree/master/binding-library/java), the prerequisite is required.

However, If you can use [DevContainer](https://code.visualstudio.com/docs/remote/containers), you don't need to prepare the development environment. For the prerequisite for the devcontainer is: 

* [Docker for Windows](https://docs.docker.com/docker-for-windows/) or [Docker for Mac](https://docs.docker.com/docker-for-mac/install/)
* [Visual Studio Code](https://code.visualstudio.com/)
* [Visual Studio Code - Remote Development extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.vscode-remote-extensionpack)

DevContainer will set up all of the prerequisite includs [AzureCLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) with local Kafka Cluster.

## Update Binding Library (Optional)

If you want to update the binding library, Follow the instruction of [Kafka binding library for java](https://github.com/Azure/azure-functions-kafka-extension/tree/master/binding-library/java).

## Start the DevContainer

Go to the `samples/java` directory then open the Visual Studio Code. 

```
$ cd samples/java
$ code . 
```

Visual Studio might automatically ask you to start container, if not, you can click the right bottom green icon (><) then you will see the following dropd down.

![Remote Container](../../docs/images/RemoteContainer.png)

Select `Remote-Containers: Reopen in Container`. It start the DevContainer, wait a couple of minutes, you will find a java development enviornment and a local kafka cluster is already up with Visual Studio Code.

### Modify the sample

The sample is already created and configured. However, you might want to change the configurateion. For example, the Function App name. It might  conflict with our function app. Let's change the config to fit your enviornment.

### Modify pom.xml (optional)

Go to the sample app directory, buid the sample. If you want to change the function App that you want to deploy this app, change the following section on the `pom.xml` file. The current `pom.xml` file is for windows and deploy windows based function app. If you are using mac, you can rename the `pom_linux.xml` to `pom.xml`. It deploys linux based function app.

_pom.xml_  

```xml
        <functionAppName>kafka-function-20190419163130420</functionAppName>
        <functionAppRegion>westus</functionAppRegion>
        <stagingDirectory>${project.build.directory}/azure-functions/${functionAppName}</stagingDirectory>
        <functionResourceGroup>java-functions-group</functionResourceGroup>
```

### Modify install_extension shell (Optional)

Modify the functionAppName of `install_extension.ps1` (windows) or `install_extension.sh` (mac/bash) if you change the functionAppName.

_isntall_extension.ps1_

```powershell
$FunctionAppName = "kafka-function-20190419163130420"
```

_install_extension.sh_

```bash
FUNCTION_APP_NAME=kafka-function-20190419163130420
```

### Modify devcontainer.json (Optional)

Modify the following part if you modify the functionAppName.

```json
"remoteEnv": {"LD_LIBRARY_PATH": "/workspace/target/azure-functions/kafka-function-20190419163130420/bin/runtimes/linux-x64/native"},
"containerEnv": {"LD_LIBRARY_PATH": "/workspace/target/azure-functions/kafka-function-20190419163130420/bin/runtimes/linux-x64/native"},
```

### Three Smaples

This sample contains three functions. `Kafka Cluster` local means, it uses a kafka cluster that is started with DevContainer.

| Name | Description | Kafka Cluster| Comment out |
| ----- | --------------- | -------| ---|
| KafaInput-Java | Output binding for sending messsages to a kafka cluster. | local | No |
| KafkaTrigger-Java | Simple Kafka trigger sample | local | No |
| KafkaTrigger-Java-Many | Kafka batch processing sample with Confluent Cloud | Confluent Cloud | yes |

### Modify TriggerFunction.java (Windows user only)

If you want to run sample on your Windows with Confluent Cloud and you are not using DevContainer, uncomment the following line. It is the settings of CA certificate. .NET Core, that is azure functions host language, can not access windows registry, that means can not access the CA certificate of the Confluent Cloud.

```java
sslCaLocation = "confluent_cloud_cacert.pem",
```

### Build and package the app

```bash
$ mvn clean package
```

## Install the KafkaTriggerExtension

Run install extension script for installing kafka extension. For windows, It also install `confluent_cloud_cacert.pem`.

_windows_

```powershell
PS > .\isntall_extension.ps1
```

_mac or bash_

```bash
$ ./install_extension.sh
```

Check if there is dll packages under the `target/azure-functions/kafka-function-(some number)/bin`. If it is sucess, you will find `Microsoft.Azure.WebJobs.Extensions.Kafka.dll` on it. 

## Run the Azure Functions

## Configration (Optional)
Copy the `local.settings.json.example` to `local.settings.json` and configure it. This configuration requires for `KafkaTrigger-Java-Many` sample that works with confluent cloud. 

_local.settings.json_
```
{
    "IsEncrypted": false,
    "Values": {
    "BrokerList": "YOUR_BROKER_LIST_HERE",
    "ConfluentCloudUserName": "YOUR_CONFLUENT_USER_NAME_HERE",
    "ConfluentCloudPassword": "YOUR_CONFLUENT_PASSWORD_HERE"
    }
}
```

## Run 

This sample includes two samples. Go to `TriggerFunction.java`. You will find `KafkaTrigger-Java` and `KafkaTrigger-Java-Many`. `KafkaTrigger-Java` is a sample for local kafka cluster. `KafkaTrigger-Java-Many` is a sample for [Confluent Cloud in Azure](https://github.com/Azure/azure-functions-kafka-extension#connecting-to-confluent-cloud-in-azure) with batch execution. If you execute this sample as-is, one of the function will cause an error since these 
are the different configuration. Remove or comment one of them and enjoy.

Before running the kafka extension, you need to configure `LD_LIBRARY_PATH` to the `target/azure-functions/kafka-function-20190419163130420`. For the DevContainer, the configuration is reside in the `devontainer.json`. You don't need to configure it. 

```
$ mvn azure-functions:run
```

## Deploy to Azure

If you want to deploy your functions to Azure, modify your `pom.xml`. Find two pieces to configure it. 

### Configuration 

#### FunctionApp name, region, and resource group

Change the these info if you alreay have a function app. 

_pom.xml_

```xml
        <functionAppName>kafka-function-20190419163130420</functionAppName>
        <functionAppRegion>westus</functionAppRegion>
        <stagingDirectory>${project.build.directory}/azure-functions/${functionAppName}</stagingDirectory>
        <functionResourceGroup>java-functions-group</functionResourceGroup>
```

#### os and pricing tier (premium plan)

_pom.xml_

```xml
 
                    <runtime><os>windows</os></runtime>
                    <pricingTier>EP1</pricingTier>
```

### deploy to Azure

#### deploy the app
The maven target will create a function app if it is not exists then deploy the application. 

```
$ mvn azure-functions:deploy -e
```

#### Configure AppSettings 

Go to Azure Portal, select the FunctionAPp, then go to Configuration > Application settings. You need to configure these application settings. `BrokerList`, `ConfluentCloudUsername` and `ConfluentCloudPassowrd` are required for the sample. 
`LD_LIBRARY_PATH` is required for Linux based Function App. That is references so library that is included on the Kafka extensions. 

| Name | Description | NOTE |
| BrokerList | Kafka Broker List | e.g. changeme.eastus.azure.confluent.cloud:9092 |
| ConfluentCloudUsername | Username of Confluent Cloud | - |
| ConfluentCloudPassword | Password of Confluent Cloud | - |
| LD_LIBRARY_PATH | /home/site/wwwroot/bin/runtimes/linux-x64/native | Linux only |

#### Send kakfka event

Send kafka events from producer. You can call `KafkaInput-Java` for a local cluster, or you can use [ccloud](https://docs.confluent.io/current/cloud/cli/index.html) command for confluent cloud.

```bash
$ ccloud login
$ ccloud kafka topic produce message
```

For more details, Go to [ccloud](https://docs.confluent.io/current/cloud/cli/command-reference/ccloud.html).

If you want to send event to the local kafka cluster, you can use
[kafakacat](https://docs.confluent.io/current/app-development/kafkacat-usage.html) instead.

```bash
$ apt-get update && apt-get install kafkacat
$ kafkacat -b broker:29092 -t users -P
```

# Resource

* [Quickstart: Create a function in Azure that responds to HTTP requests](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli?pivots=programming-language-java&tabs=bash%2Cbrowser)
* [azure-maven-plugins: AzureFunctions: Configuration Details](https://github.com/microsoft/azure-maven-plugins/wiki/Azure-Functions:-Configuration-Details)
* [Confluent cloud Quick Start](https://docs.confluent.io/current/quickstart/cloud-quickstart/index.html#cloud-quickstart)

