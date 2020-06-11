# Developers guid for Kafka Functions for Java

Explain how to compile the Java bindings and run the sample. 

## Prerequiste 

Refer to the [Readme](https://github.com/Azure/azure-functions-kafka-extension/tree/master/binding-library/java)

## Build the binding-libray/java

Clone this repo then go to binding-library. 

```
$ git clone https://github.com/Azure/azure-functions-kafka-extension.git
$ cd binding-library/java
```

### Build a package 

```
$ mvn clean 
$ mvn package
```

### Install package 

Install the binding library to the sample app. Please modify the version.

```
$ mvn install:install-file -Dfile=target/azure-functions-java-library-kafka-1.0.0.jar -DgroupId=com.microsoft.azure.functions -DartifactId=azure-functions-java-library-kafka -Dversion=1.0.0 -Dpackaging=jar -DlocalRepositoryPath=../../samples/java/src/repo/
```

## Build the sample 

### Go to sample directory

```
$ cd ../../sample
```

### Modify pom.xml (optional)

Go to the sample app directory, buid the sample. If you want to change the function App that you want to deploy this app, change the following section on the `pom.xml` file. The current `pom.xml` file is for windows and deploy windows based function app. If you are using mac, you can rename the `pom_linux.xml` to `pom.xml`. It deploys linux based function app.

_pom.xml_  

```xml
        <functionAppName>kafka-function-20190419163130420</functionAppName>
        <functionAppRegion>westus</functionAppRegion>
        <stagingDirectory>${project.build.directory}/azure-functions/${functionAppName}</stagingDirectory>
        <functionResourceGroup>java-functions-group</functionResourceGroup>
```

### Modify install_extension shell

Modify the functionAppName of `install_extension.ps1` (windows) or `install_extension.sh` (mac/bash) if you change the functionAppName. 

_isntall_extension.ps1_

```powershell
$FunctionAppName = "kafka-function-20190419163130420"
```

_install_extension.sh_

```bash
FUNCTION_APP_NAME=kafka-function-20190419163130420
```

### Modify TriggerFunction.java (Mac/Linux user only)

Remove following line. This line is windows only. 

```java                 
sslCaLocation = "confluent_cloud_cacert.pem", // Remove this attribute for Linux/Mac users.
```

### Build and package the app

```
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

This sample includes two samples. Go to `TriggerFunction.java`. You will find `KafkaTrigger-Java` and `KafkaTrigger-Java-Many`. `KafkaTrigger-Java` is a sample for local kafka cluster. `KafkaTrigger-Java-Many` is a sample for confluent cloud with batch execution. If you execute this sample as-is, one of the function will cause an error since these 
are the different configuration. Remove or comment one of them and enjoy. 

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
# Resource

* [Quickstart: Create a function in Azure that responds to HTTP requests](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-first-azure-function-azure-cli?pivots=programming-language-java&tabs=bash%2Cbrowser)
* [azure-maven-plugins: AzureFunctions: Configuration Details](https://github.com/microsoft/azure-maven-plugins/wiki/Azure-Functions:-Configuration-Details)
* [Confluent cloud Quick Start](https://docs.confluent.io/current/quickstart/cloud-quickstart/index.html#cloud-quickstart)

