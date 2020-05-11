# Kafka Trigger for Java Deverlopers note

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

Go to the sample app directory, buid the sample.

```
$ cd ../../sample
$ mvn clean
$ mvn package
```

## Install the KafkaTriggerExtension 

Go to the Azure Functions directory that is created by the package command. Please change the number following `kafka-function-` according to your environment. 

```
$ cd target/azure-functions/kafka-function-20190419163130420/
$ func extensions install Microsoft.Azure.WebJobs.Extensions.Kafka --version 2.0.0-beta
```

Check if there is dll packages under the `target/azure-functions/kafka-function-(some number)/bin`. If it is sucess, you will find `Microsoft.Azure.WebJobs.Extensions.Kafka.dll` on it. 

## Run the Azure Functions

```
$ mvn azure-functions:run
```

