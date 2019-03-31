Azure Functions extensions for Apache Kafka
===
|Branch|Status|
|---|---|
|master|[![Build Status](https://dev.azure.com/ryancraw/azure-functions-kafka-extension/_apis/build/status/Microsoft.azure-functions-kafka-extension?branchName=master)](https://dev.azure.com/ryancraw/azure-functions-kafka-extension/_build/latest?definitionId=2?branchName=master)

This repository contains Kafka binding extensions for the **Azure WebJobs SDK**. The extension status is experimental/under development. The communcation with Kafka is based on library **Confluent.Kafka version 1.0.0-beta3**.

**DISCLAIMER**: This library is under development and is experimental. Currently there is no guarantee regarding support or availability as a product.

## Bindings

There are two binding types in this repo: trigger and output (still to come). To get started using the extension in a WebJob project add reference to Microsoft.Azure.WebJobs.Extensions.Kafka project and call `AddKafka()` on the startup:

```csharp
static async Task Main(string[] args)
{
  var builder = new HostBuilder()
        .UseEnvironment("Development")
        .ConfigureWebJobs(b =>
        {
            b.AddKafka();
        })
        .ConfigureAppConfiguration(b =>
        {
        })
        .ConfigureLogging((context, b) =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddConsole();
        })
        .ConfigureServices(services =>
        {
            services.AddSingleton<Functions>();
        })
        .UseConsoleLifetime();

    var host = builder.Build();
    using (host)
    {
        await host.RunAsync();
    }
}

public class Functions
{
    const string Broker = "localhost:9092";
    const string StringTopicWithOnePartition = "stringTopicOnePartition";
    const string StringTopicWithTenPartitions = "stringTopicTenPartitions";

    /// <summary>
    /// Trigger for the topic
    /// </summary>
    public void MultiItemTriggerTenPartitions(
        [KafkaTrigger(Broker, StringTopicWithTenPartitions, ConsumerGroup = "myConsumerGroup")] KafkaEventData[] events,
        ILogger log)
    {
        foreach (var kafkaEvent in events)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
        }
    }
}
```

### Trigger Binding

Trigger bindings are designed to consume messages from a Kafka topics.

```csharp
public static void StringTopic(
    [KafkaTrigger("BrokerList", "myTopic", ConsumerGroup = "myGroupId")] KafkaEventData[] kafkaEvents,
    ILogger logger)
{
    foreach (var kafkaEvent in kafkaEvents)
        logger.LogInformation(kafkaEvent.Value.ToString());
}
```

Kafka messages can be serialized in multiple formats. Currently the following formats are supported: string, Avro and Protobuf.

#### Avro Binding Support

The Kafka trigger supports two methods for consuming Avro format:

- Specific: where the concrete user defined class will be instantiated and filled during message deserialization
- Generic: where the user provides the avro schema and a generic record is created during message deserialization

*Using Avro specific*

1. Define a class that inherits from `ISpecificRecord`.
1. In `KafkaTrigger` attribute set the `ValueType` of the class defined in previous step
1. The parameter type used with the trigger must be of type `KafkaEventData`. The value of `KafkaEventData.Value` will be of the specified type.

```csharp
public class UserRecord : ISpecificRecord
{
    public const string SchemaText = @"    {
  ""type"": ""record"",
  ""name"": ""UserRecord"",
  ""namespace"": ""KafkaFunctionSample"",
  ""fields"": [
    {
      ""name"": ""registertime"",
      ""type"": ""long""
    },
    {
      ""name"": ""userid"",
      ""type"": ""string""
    },
    {
      ""name"": ""regionid"",
      ""type"": ""string""
    },
    {
      ""name"": ""gender"",
      ""type"": ""string""
    }
  ]
}";
    public static Schema _SCHEMA = Schema.Parse(SchemaText);

    [JsonIgnore]
    public virtual Schema Schema => _SCHEMA;
    public long RegisterTime { get; set; }
    public string UserID { get; set; }
    public string RegionID { get; set; }
    public string Gender { get; set; }

    public virtual object Get(int fieldPos)
    {
        switch (fieldPos)
        {
            case 0: return this.RegisterTime;
            case 1: return this.UserID;
            case 2: return this.RegionID;
            case 3: return this.Gender;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
        };
    }
    public virtual void Put(int fieldPos, object fieldValue)
    {
        switch (fieldPos)
        {
            case 0: this.RegisterTime = (long)fieldValue; break;
            case 1: this.UserID = (string)fieldValue; break;
            case 2: this.RegionID = (string)fieldValue; break;
            case 3: this.Gender = (string)fieldValue; break;
            default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
        };
    }
}


public static void User(
    [KafkaTrigger("BrokerList", "users", ValueType=typeof(UserRecord), ConsumerGroup = "myGroupId")] KafkaEventData[] kafkaEvents,
    ILogger logger)
{
    foreach (var kafkaEvent in kafkaEvents)
    {
        var user = (UserRecord)kafkaEvent.Value;
        logger.LogInformation($"{JsonConvert.SerializeObject(kafkaEvent.Value)}");
    }
}
```

*Using Avro Generic*

1. In `KafkaTrigger` attribute set the value of `AvroSchema` to the string representation of it.
1. The parameter type used with the trigger must be of type `KafkaEventData`. The value of `KafkaEventData.Value` will be of the type `GenericRecord`.

The sample function contains 1 consumer using avro generic. Check the class `AvroGenericTriggers`

```csharp
public static class AvroGenericTriggers
{
      const string PageViewsSchema = @"{
  ""type"": ""record"",
  ""name"": ""pageviews"",
  ""namespace"": ""ksql"",
  ""fields"": [
    {
      ""name"": ""viewtime"",
      ""type"": ""long""
    },
    {
      ""name"": ""userid"",
      ""type"": ""string""
    },
    {
      ""name"": ""pageid"",
      ""type"": ""string""
    }
  ]
}";

[FunctionName(nameof(PageViews))]
public static void PageViews(
    [KafkaTrigger("BrokerList", "pageviews", AvroSchema = PageViewsSchema, ConsumerGroup = "myGroupId")] KafkaEventData kafkaEvent,
    ILogger logger)
{
    if (kafkaEvent.Value is GenericRecord genericRecord)
    {
        // Get the field values manually from genericRecord
    }
}
```

#### Protobuf Binding Support

Protobuf is supported in the trigger based on the `Google.Protobuf` nuget package. To consume a topic that is using protobuf as serialization set the ValueType to be of a type that implements `Google.Protobuf.IMessage`. The sample producer has a producer for topic `protoUser` (must be created). The sample function has a trigger handler for this topic in class `ProtobufTriggers`.

```csharp
public static class ProtobufTriggers
{
    [FunctionName(nameof(ProtobufUser))]
    public static void ProtobufUser(
        [KafkaTrigger("BrokerList", "protoUser", ValueType=typeof(ProtoUser), ConsumerGroup = "myGroupId")] KafkaEventData[] kafkaEvents,
        ILogger logger)
    {
        foreach (var kafkaEvent in kafkaEvents)
        {
            var user = (ProtoUser)kafkaEvent.Value;
            logger.LogInformation($"{JsonConvert.SerializeObject(user)}");
        }
    }
}
```

### Output Binding

Output binding are designed to produce messages to a Kafka topic. It supports different keys and values types. Avro and Protobuf serialisation are built-in.

```csharp
[FunctionName("ProduceStringTopic")]
public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
    [Kafka("stringTopicTenPartitions", BrokerList = "LocalBroker")] IAsyncCollector<KafkaEventData> events,
    ILogger log)
{
    var kafkaEvent = new KafkaEventData()
    {
        Value = await new StreamReader(req.Body).ReadToEndAsync(),
    };

    await events.AddAsync(kafkaEvent);

    return new OkResult();
}
```

To set a key value set the property `KeyType` (i.e typeof(string) or typeof(long)).

To send use Protobuf serialisation set the value of the KafkaAttribute.ValueType to a type that implements the IMessage.

For Avro provide a type that implements ISpecificRecord.
If nothing is defined the value will be of type string and no key will be set.

## Configuration

Customizing the Kafka extensions is available in the host file. As mentioned before, the interface to Kafka is built based on **Confluent.Kafka** library, therefore some of the configuration is just a bridge to the producer/consumer.

```json
{
  "version": "2.0",
  "extensions": {
    "kafka": {
      "maxBatchSize": 100
    }
  }
}
```

### Configuration Settings

Confluent.Kafka is based on librdkafka C library. Some of the configuration required by the library is exposed by the extension in this repository. The complete configuration for librdkafka can be found [here](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md).

#### Extension configuration

|Setting|Description|Default Value
|-|-|-|
|MaxBatchSize|Maximum batch size when calling a Kafka trigger function|64
|SubscriberIntervalInSeconds|Defines the minimum frequency in which messages will be executed by function. Only if the message volume is less than MaxBatchSize / SubscriberIntervalInSeconds|1
|ExecutorChannelCapacity|Defines the channel capacity in which messages will be sent to functions. Once the capacity is reached the Kafka subscriber will pause until the function catches up|10
|ChannelFullRetryIntervalInMs|Defines the interval in milliseconds in which the subscriber should retry adding items to channel once it reaches the capacity|50

#### librdkafka configuration

The settings exposed here are targeted to more advanced users that want to customize how librdkafka works. Please check the librdkafka [documentation](https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md) for more information.

|Setting|librdkafka property|Trigger or Output|
|-|-|-|
|ReconnectBackoffMs|reconnect.backoff.max.ms|Trigger
|ReconnectBackoffMaxMs|reconnect.backoff.max.ms|Trigger
|StatisticsIntervalMs|statistics.interval.ms|Trigger
|SessionTimeoutMs|session.timeout.ms|Trigger
|MaxPollIntervalMs|max.poll.interval.ms|Trigger
|QueuedMinMessages|queued.min.messages|Trigger
|QueuedMaxMessagesKbytes|queued.max.messages.kbytes|Trigger
|MaxPartitionFetchBytes|max.partition.fetch.bytes|Trigger
|FetchMaxBytes|fetch.max.bytes|Trigger

If you are missing an configuration setting please create an issue and describe why you need it.

## Quickstart

For samples take a look at the [samples folder](./samples).