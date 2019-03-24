# Development Guide

This document helps setting a development environment using Docker-Compose.

## Using Confluent Kafka with Docker Compose

Follow the [guide](https://docs.confluent.io/current/quickstart/ce-docker-quickstart.html#cp-quick-start-docker) to setup a local Kafka with docker-compose.

Complete the steps in the guide at least until the topics pageviews, users and pageviews_female are created (including data generators). The included sample function contains a consumer for each of those 3 topics.

## Sample function

A sample function is provided in folder sample/KafkaFunctionSample. It depends on the Kafka installed locally (broker:9092), as described in previous section. The local.settings.json should look similar to this:

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

If localhost does not work try to use `broker` and add it in hosts file as `127.0.0.1`.

## Avro handling

In order to handle avro the Kafka extension supports two methods:

- Specific: where the concrete user defined class will be instantiated and filled during message deserialization
- Generic: where the user provides the avro schema and a generic record is created during message deserialization

### Setting up Avro Specific

1. Define a class that inherits from `ISpecificRecord`.
1. In `KafkaTrigger` attribute set the `ValueType` of the class defined in previous step
1. The parameter type used with the trigger must be of type `KafkaEventData`. The value of `KafkaEventData.Value` will be of the specified type.

The sample function contains 2 consumers using specific avro. Check the class `AvroSpecificTriggers`.

### Setting up Avro Generic

1. In `KafkaTrigger` attribute set the value of `AvroSchema` to the string representation of it.
1. The parameter type used with the trigger must be of type `KafkaEventData`. The value of `KafkaEventData.Value` will be of the type `GenericRecord`.

The sample function contains 1 consumer using avro generic. Check the class `AvroGenericTriggers`

## Protobuf

Protobuf is supported in the trigger. The implementation is based in package `Google.Protobuf`. To consume a topic that is using protobuf as serialization set the ValueType to be of a type that implements `Google.Protobuf.IMessage`. The sample producer has a producer for topic `protoUser` (must be created). The sample function has a trigger handler for this topic in class `ProtobufTriggers` (you need to commented out).

## Additional information

### Building a WebJobs trigger

Short description of building a web jobs trigger:

1. Setup a WebJobsStartup using the `WebJobsStartup` attribute on assembly level. During the initialisation add an extension `IWebJobsBuilder.AddExtension<T>`
1. In `IWebJobsStartup.Configure` register a `IExtensionConfigProvider`
1. In `IExtensionConfigProvider` add the bindings for the required attributes. For input triggers (starting a functions execution) a `ITriggerBindingProvider` implementation is required
1. In `ITriggerBindingProvider` implementation we need to map a `IListener` for function parameters
1. In `IListener` implementation we call `ITriggeredFunctionExecutor.TryExecuteAsync` to trigger the function call

### Kafka trigger

Triggering function calls starts at the KafkaListener class. It is responsible for connecting a Kafka trigger function with a Kafka Consumer (from Confluent.Kafka library).
As the library does not seem to support a way to receive message in batches the current implementation loops getting items with a timeout, triggering the functions as messages are received.
At the listener startup a "Kafka subscriber thread" is created.

The subscriber thread does the following:

```
while (!cancellationToken.IsCancellationRequested)
    while !MaxBatchReleaseTime is passed
        kafkaItem = read_from_kafka()
        executor.add(kafkaItem)
        
        if (executor.length > maxBatchSize) {
            executor.flush() // making the items available for the function executor
            alreadyFlushedInCurrentExecution = true
        }
    end
    
    if (!alreadyFlushedInCurrentExecution)
        executor.flush()
end
```

Function executors:

The function is executed by two type of executors: SingleItem and MultiItem.
A single item executor calls the received items in a loop, one by one in parallel if multiple partitions exist. The multi item executor calls the function a single time, items will be in order, batch can contain items from distinct partitions. Once all items have been processed the checkpoint is saved.

Items to send to function are added to a channel with a capacity of 10 batches (10 * ~64 items). Once full the subscriber will pause until the function catches up. This will cause all partitions to wait.

### Execution order

**Data in Kafka**

```
Partition 1: ABCDE
Partition 2: 12345
```

**Receiving order**
```
00:00:01: AB12C
00:00:02: 34DE5
```

**Multi item function**

1. Batch AB12C is received
1. Send to function batch containing AB12C
1. Commit C and 2
1. Batch 34DE5 is received
1. Send to function batch containing 34DE5
1. Commit E and 5

**Single item function**

1. Batch AB12C is received
1. Sends A, 1 (in parallel)
1. Sends B, 2 (in parallel)
1. Sends C
1. Commits C and 2
1. Batch 34DE5 is received
1. Sends D, 3 (in parallel)
1. Sends E, 4 (in parallel)
1. Sends 5
1. Commits E and 5
