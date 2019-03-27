Azure Functions extensions for Apache Kafka
===
|Branch|Status|
|---|---|
|master|[![Build Status](https://dev.azure.com/ryancraw/azure-functions-kafka-extension/_apis/build/status/Microsoft.azure-functions-kafka-extension?branchName=master)](https://dev.azure.com/ryancraw/azure-functions-kafka-extension/_build/latest?definitionId=2?branchName=master)

This repository contains Kafka binding extensions for the **Azure WebJobs SDK**. The extension status is experimental/under development. 

[TODO: add more information about usage/limitations]

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

Trigger bindings are design for the consumption of Kafka topics.

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

[TODO: add output binding documentation]

## Quickstart

For samples take a look at the [samples folder](./samples).

## License

[TODO: add license / use the dotnetfoundation one?]