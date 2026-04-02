# KafkaRecord Binding — Design & Architecture

> **Status**: Phase 2 of [#612](https://github.com/Azure/azure-functions-kafka-extension/issues/612)
> **Scope**: .NET Isolated Worker (Phase 2); cross-language support planned (Phase 4+)

## Overview

`KafkaRecord` is a new binding type that gives Azure Functions users access to the complete Apache Kafka record metadata — topic, partition, offset, key (raw bytes), value (raw bytes), headers, timestamp, and leader epoch — without coupling to the Confluent.Kafka library.

Users opt in by changing their function parameter type. Existing bindings (`string`, `byte[]`, `KafkaEventData<T>`) are completely unaffected.

```csharp
// Existing (unchanged)
[Function("StringTrigger")]
public void Run([KafkaTrigger("brokers", "topic", ConsumerGroup = "group")] string message) { }

// NEW: Full record metadata
[Function("RecordTrigger")]
public void Run([KafkaTrigger("brokers", "topic", ConsumerGroup = "group")] KafkaRecord record)
{
    string key = Encoding.UTF8.GetString(record.Key);
    var value = JsonSerializer.Deserialize<MyEvent>(record.Value);
    Console.WriteLine($"Topic={record.Topic} Partition={record.Partition} Offset={record.Offset}");

    foreach (var header in record.Headers)
        Console.WriteLine($"  {header.Key} = {header.GetValueAsString()}");
}

// NEW: Batch mode
[Function("BatchTrigger")]
public void Run(
    [KafkaTrigger("brokers", "topic", ConsumerGroup = "group", IsBatched = true)]
    KafkaRecord[] records) { }
```

---

## Design Rationale

### Why not expose `ConsumeResult<TKey, TValue>` directly?

The Confluent.Kafka `ConsumeResult<TKey, TValue>` type:
- Has no public constructor — cannot be reconstructed from serialized bytes across the gRPC process boundary
- Represents a **consumer state** (includes `IsPartitionEOF`), not a pure Kafka record
- Uses generics that don't support `TopicRecordName` strategy (multiple schemas per topic)
- Couples users to the Confluent.Kafka library version shipped with the extension

### The `KafkaRecord` approach

Based on customer feedback ([#612 discussion](https://github.com/Azure/azure-functions-kafka-extension/issues/612)):

| Design Decision | Rationale |
|----------------|-----------|
| **No generics** — Key and Value are `byte[]` | User controls deserialization; supports any schema strategy |
| **Apache Kafka spec-aligned** | Fields match the Kafka protocol record definition |
| **`IsPartitionEOF` excluded** | Consumer state, not record metadata |
| **Protobuf serialization** | Zero Base64 overhead for binary key/value transport |
| **Opt-in via parameter type** | No configuration changes; existing code unaffected |

---

## Architecture

### Data Flow

```
Host Process                                    Worker Process
┌─────────────────────────────┐                ┌─────────────────────────────┐
│ KafkaListener               │                │ KafkaRecordConverter        │
│  │                          │                │  (IInputConverter)          │
│  ▼                          │                │  │                          │
│ ConsumeResult<TKey,TValue>  │                │  ▼                          │
│  │                          │                │ KafkaRecordProto            │
│  ▼                          │    gRPC        │  │                          │
│ KafkaRecordProtobufSerializer ──────────────→│  ▼                          │
│  │                          │ ModelBinding   │ KafkaRecord (POCO)          │
│  ▼                          │ Data (bytes)   │  │                          │
│ ParameterBindingData        │                │  ▼                          │
│  source: "AzureKafkaRecord" │                │ User Function               │
│  content_type: protobuf     │                │                             │
└─────────────────────────────┘                └─────────────────────────────┘
```

### Key Components

| Component | Repository | Role |
|-----------|-----------|------|
| `KafkaRecordProtobufSerializer` | azure-functions-kafka-extension | Serializes `IKafkaEventData` → Protobuf bytes |
| `KafkaRecordProto.proto` | Both repos (identical schema) | Protobuf schema definition |
| `KafkaEventDataConvertManager` | azure-functions-kafka-extension | Routes `ParameterBindingData` conversion |
| `KafkaRecordConverter` | azure-functions-dotnet-worker | Deserializes Protobuf → `KafkaRecord` POCO |
| `KafkaRecord` | azure-functions-dotnet-worker | User-facing POCO type |

### Protobuf Schema

```protobuf
message KafkaRecordProto {
    string topic = 1;
    int32 partition = 2;
    int64 offset = 3;
    bytes key = 4;              // native bytes, zero overhead
    bytes value = 5;            // native bytes, zero overhead
    KafkaTimestampProto timestamp = 6;
    repeated KafkaHeaderProto headers = 7;
    optional int32 leader_epoch = 8;
}
```

The Host does not inspect `ModelBindingData.content` — it is an opaque pass-through. This means:
- No Azure Functions Host changes required
- No Host release dependency
- The serialization format is an internal contract between the Kafka Extension and Worker Extension only

---

## Configuration

**No new configuration is needed.** The binding type is determined by the function parameter type at build time:

| Parameter Type | Binding Path | Serialization |
|---------------|-------------|---------------|
| `string` | Value → string | None |
| `byte[]` | Value → byte array | None |
| `KafkaEventData<T>` | Full event → JSON | JSON |
| **`KafkaRecord`** | **Full record → Protobuf** | **Protobuf** |

All existing `host.json` settings (`extensions.kafka.*`), trigger attributes (`BrokerList`, `Topic`, `ConsumerGroup`, SASL/SSL, etc.), and scaling configuration (`LagThreshold`) work identically regardless of the binding type.

---

## Impact Analysis

### Scale Controller: No Impact

The Scale Controller uses `KafkaTriggerMetrics` (TotalLag, PartitionCount) via `IScaleMonitor`, which queries Kafka broker metadata directly. It never touches `ModelBindingData` or cares about the user's binding type.

### Existing Bindings: No Impact

`KafkaRecord` is a new, parallel code path activated only when the user's function parameter is `KafkaRecord` or `KafkaRecord[]`. The existing `string`, `byte[]`, and `KafkaEventData<T>` paths are completely untouched.

### Performance: Opt-in, Marginal Overhead

| Aspect | Impact |
|--------|--------|
| **Users NOT using `KafkaRecord`** | Zero impact — code path unchanged |
| **Users using `KafkaRecord`** | Marginal increase in gRPC payload (metadata: ~50-200 bytes fixed overhead per message) |
| **Serialization cost** | Protobuf is faster than JSON+Base64 alternative; comparable to EventHubs/ServiceBus SDK type binding |
| **Memory** | One additional `KafkaRecord` allocation per message (lightweight POCO) |

For a 1KB message payload, the metadata overhead is ~5-10%. For larger messages, it becomes negligible.

### Comparison with Other Extensions

| Extension | SDK Type Binding | Serialization | Performance Pattern |
|-----------|-----------------|---------------|-------------------|
| EventHubs | `EventData` | AMQP binary | SDK type via `AmqpAnnotatedMessage` |
| ServiceBus | `ServiceBusReceivedMessage` | AMQP binary | SDK type via `AmqpAnnotatedMessage` |
| **Kafka** | **`KafkaRecord`** | **Protobuf binary** | **Same pattern, different wire format** |

### Dependencies Added

| Package | Kafka Extension (Host) | Worker Extension |
|---------|----------------------|-----------------|
| `Grpc.Tools` | Yes (build-time only, `PrivateAssets=All`) | Yes (build-time only) |
| `Google.Protobuf` | No (transitive via Confluent) | Yes (explicit, no transitive path) |

No new runtime dependencies are introduced to the Kafka Extension. `Grpc.Tools` is build-time only.

---

## Cross-Repository Coordination

This feature spans two repositories that must be released together:

| Repository | Package | Changes |
|-----------|---------|---------|
| `Azure/azure-functions-kafka-extension` | `Microsoft.Azure.WebJobs.Extensions.Kafka` | Protobuf serializer, ConvertManager update |
| `Azure/azure-functions-dotnet-worker` | `Microsoft.Azure.Functions.Worker.Extensions.Kafka` | KafkaRecord types, IInputConverter |

**Release order**: Both packages must be published simultaneously. If only one side is updated:
- Host-only update: Sends `AzureKafkaRecord` Protobuf, but Worker has no converter → `KafkaRecord` binding fails (existing bindings unaffected)
- Worker-only update: Worker has converter, but Host sends old format → converter never matches (existing bindings unaffected)

In either partial-update scenario, **existing bindings continue to work** — only the new `KafkaRecord` binding would be non-functional until both sides are updated.

---

## Future Work (Phase 3+)

- **E2E tests**: Docker-based Kafka integration tests for `KafkaRecord` single and batch dispatch
- **Cross-language support**: Node.js, Java, Python, PowerShell — each language worker needs a `KafkaRecord` equivalent type and converter
- **Documentation**: README updates, binding reference, samples
