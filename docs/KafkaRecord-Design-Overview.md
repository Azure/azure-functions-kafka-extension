# KafkaRecord Design Overview

## 1. Why KafkaRecord Is Needed — Background and Motivation

### The Problem Today

The Azure Functions Kafka Trigger currently supports binding to the following types:

```csharp
// Option 1: value only (string)
public void Run([KafkaTrigger(...)] string message) { }

// Option 2: value only (binary)
public void Run([KafkaTrigger(...)] byte[] message) { }

// Option 3: KafkaEventData<TValue> (partial metadata)
public void Run([KafkaTrigger(...)] KafkaEventData<string> evt) { }
```

None of these give users access to the **complete Kafka record metadata** that they need.

### What Customers Want to Access

An Apache Kafka record carries the following information:

| Field | Use Case | Available Today? |
|---|---|---|
| **Topic** | Which topic the record came from | Partially (trigger metadata only in isolated/non-.NET; not via `string`/`byte[]` binding) |
| **Partition** | Partition number | Partially (same as above) |
| **Offset** | Position within partition | Partially (same as above) |
| **Key (raw bytes)** | Message key | Partially (type-converted, not raw bytes; `string`/`byte[]` bindings lose it entirely) |
| **Value (raw bytes)** | Message body | Partially (type-converted, not raw bytes) |
| **Headers** | Custom headers (correlation IDs, etc.) | Partially (JSON-serialized in trigger metadata; not available in `string`/`byte[]` bindings) |
| **Timestamp** | When the record was produced/appended | Partially (DateTime only; available via trigger metadata or `KafkaEventData`) |
| **Timestamp Type** | CreateTime vs LogAppendTime | No (lost during DateTime conversion in all existing paths) |
| **LeaderEpoch** | Broker leader epoch | Partially (trigger metadata only; not via `string`/`byte[]` bindings) |

### What "Partially" Means — Availability by Binding Type

Whether a field is accessible depends on **which binding type the user chooses**. The host extension internally holds all fields in `IKafkaEventData`, but how the data reaches the user varies:

| Field | `string` binding | `byte[]` binding | `KafkaEventData<T>` (in-process only) | Trigger Metadata (non-.NET / isolated) | **`KafkaRecord` (new)** |
|---|---|---|---|---|---|
| **Value** | ✅ (this is it) | ✅ (this is it) | ✅ `.Value` | ✅ body | ✅ `.Value` (raw bytes) |
| **Key** | ❌ | ❌ | ✅ `.Key` | ✅ `Key` (type-converted) | ✅ `.Key` (raw bytes) |
| **Topic** | ❌ | ❌ | ✅ `.Topic` | ✅ `Topic` | ✅ `.Topic` |
| **Partition** | ❌ | ❌ | ✅ `.Partition` | ✅ `Partition` | ✅ `.Partition` |
| **Offset** | ❌ | ❌ | ✅ `.Offset` | ✅ `Offset` | ✅ `.Offset` |
| **Timestamp** | ❌ | ❌ | ✅ `.Timestamp` (DateTime only) | ✅ `Timestamp` (string) | ✅ `.Timestamp` (ms + type) |
| **Timestamp Type** | ❌ | ❌ | ❌ (lost in DateTime conversion) | ❌ | ✅ `.Timestamp.Type` |
| **Headers** | ❌ | ❌ | ✅ `.Headers` | ✅ `Headers` (JSON) | ✅ `.Headers` (raw bytes) |
| **LeaderEpoch** | ❌ | ❌ | ✅ `.LeaderEpoch` | ✅ `LeaderEpoch` | ✅ `.LeaderEpoch` |

Key observations:

- **`string` / `byte[]`**: Only the value is available. All metadata is inaccessible.
- **`KafkaEventData<T>`**: Most fields are available, but this type only works in the **in-process model** (deprecated for .NET). It cannot cross the gRPC process boundary to the Isolated Worker.
- **Trigger Metadata**: Non-.NET languages and Isolated Worker can access individual fields via a separate trigger metadata dictionary, but not as a **single typed object**. Key/value arrive type-converted (not raw bytes), and headers are JSON-serialized.
- **Timestamp Type**: The only field that is **completely lost** across all existing paths. When `Confluent.Kafka.Timestamp` is converted to `DateTime`, the CreateTime vs LogAppendTime distinction is discarded.
- **`KafkaRecord`**: All fields are available as a single typed object with raw bytes, including Timestamp Type.

Specific customer needs:

1. **Correlation ID processing** — Extract trace context and correlation IDs from headers
2. **Precise offset management** — Manual commit and idempotency patterns
3. **Partition-aware logic** — Sharding strategies based on partition info
4. **Integration with existing Kafka libraries** — Internal libraries that expect raw records
5. **Zero-copy binary data access** — Self-deserialize with Avro / Protobuf / MessagePack

### Comparison with Other Triggers

EventHubs and ServiceBus already support binding to raw SDK types:

| Trigger | Abstracted Type | Raw SDK Type |
|---|---|---|
| EventHubs | Custom types | `EventData` ✅ |
| ServiceBus | Custom types | `ServiceBusReceivedMessage` ✅ |
| **Kafka** | `KafkaEventData<T>` | Raw record ❌ (added by this feature) |

Kafka was the only one left behind.

---

## 2. Key Design Decisions

### Evolution of the Design (Issue #612 Discussion)

The design evolved through customer feedback:

1. **Initial proposal**: `KafkaConsumeResult<TKey, TValue>` — mirrors Confluent.Kafka's `ConsumeResult`
2. **@daankets' proposal**: `KafkaRecord` — aligned with Apache Kafka spec, no Confluent coupling, raw bytes
3. **@KristofVDV's feedback**: Avoid Base64 overhead in JSON serialization
4. **Final decision**: `KafkaRecord` with **Protobuf** serialization via `ModelBindingData.content` (native `bytes`, zero overhead)

### Why `KafkaRecord` Instead of `KafkaConsumeResult`

| Decision | Reason |
|---|---|
| **Apache Kafka spec-aligned** | `ConsumeResult` includes consumer state (e.g., `IsPartitionEOF`). `KafkaRecord` represents the Kafka record itself |
| **`IsPartitionEOF` excluded** | This is consumer state metadata, not record metadata |
| **No generics** | Key/Value are fixed as `byte[]`. The user controls deserialization. This eliminates any dependency on Confluent.Kafka's type system |
| **No Confluent.Kafka dependency** | The worker process (user's code) is not forced to depend on the `Confluent.Kafka` package |

### Why Not Bring `KafkaEventData<T>` to Isolated Worker

A natural question is: why not simply expose the existing `KafkaEventData<TKey, TValue>` type in the Isolated Worker, rather than creating an entirely new `KafkaRecord` type? There are five fundamental reasons.

#### 1. Generics Cannot Cross the gRPC Process Boundary

`KafkaEventData<TKey, TValue>` holds Key and Value as already-deserialized typed objects:

```csharp
// Host side: TKey/TValue are resolved at construction time
public KafkaEventData(ConsumeResult<TKey, TValue> consumeResult)
{
    this.Key = consumeResult.Key;     // already TKey
    this.Value = consumeResult.Value; // already TValue
}
```

In the Isolated Worker model, Host and Worker are separate processes communicating over gRPC. To cross this boundary, data must be serialized. The problem:

- The Host does not know the user's `TValue` type (e.g., `MyCustomEvent`). That type only exists in the Worker's assembly.
- There is no mechanism to preserve and restore generic type information across gRPC.
- The Host cannot serialize an object whose type it does not have access to.

#### 2. Double Deserialization Is Wasteful

If `KafkaEventData<T>` were used in Isolated Worker, the data flow would be:

```
[Host]  Kafka bytes → Confluent Deserializer → TValue (deserialized)
                       → serialize TValue back to bytes for gRPC →
[Worker] → deserialize bytes → TValue again
```

This means deserialization happens twice — once in the Host (by Confluent.Kafka) and once in the Worker (to reconstruct the user's type). The intermediate serialize-deserialize round-trip is pure overhead.

`KafkaRecord` eliminates this by keeping key/value as raw `byte[]` throughout:

```
[Host]  Kafka bytes → wrap in Protobuf (bytes stay as-is) → gRPC →
[Worker] → Protobuf decode → byte[] → user deserializes once
```

Deserialization happens exactly once, in the Worker, under the user's control.

#### 3. Confluent.Kafka Dependency Would Be Forced on Users

`KafkaEventData<T>` references types from the Confluent.Kafka ecosystem (`Confluent.Kafka.Headers`, `Confluent.Kafka.Timestamp`, etc.). Bringing this type to the Worker extension package would pull in `Confluent.Kafka` and its native `librdkafka` binaries as transitive dependencies into every user's application — even though the Worker process never talks to Kafka directly.

`KafkaRecord` has zero dependency on `Confluent.Kafka`. Its types (`KafkaHeader`, `KafkaTimestamp`, `KafkaTimestampType`) are self-contained.

#### 4. EventHubs/ServiceBus Solved This Differently Because Their SDK Types Are Reconstructible

EventHubs and ServiceBus extensions can use the same SDK types (`EventData`, `ServiceBusReceivedMessage`) in both Host and Isolated Worker because Azure SDK types are designed to be reconstructed from their wire format (AMQP binary). For example, `ServiceBusReceivedMessage.FromAmqpMessage()` is a factory method that creates the type from serialized bytes.

`Confluent.Kafka.ConsumeResult<TKey, TValue>` has **no such factory method**. There is no way to construct a `ConsumeResult` from serialized bytes without actually consuming from a Kafka broker. (This limitation is tracked as [confluent-kafka-dotnet#2157](https://github.com/confluentinc/confluent-kafka-dotnet/issues/2157).) `KafkaEventData<T>` inherits this limitation.

#### 5. Cross-Language Unification Requires a Non-Generic Design

`KafkaEventData<TKey, TValue>` is a C# generics-based design. It cannot be naturally expressed in Python, Node.js, or Java in a way that is consistent across languages.

`KafkaRecord` (non-generic, raw bytes) maps cleanly to every language:

| Language | Type | Key/Value |
|---|---|---|
| C# | `KafkaRecord` | `byte[]` |
| Java | `KafkaRecord` (POJO) | `byte[]` |
| Python | `KafkaRecord` | `bytes` |
| Node.js | `KafkaRecord` (interface) | `Buffer` |

All languages share the same Protobuf schema and the same field set.

#### Summary

| Aspect | `KafkaEventData<T>` in Isolated | `KafkaRecord` (new design) |
|---|---|---|
| Generics across gRPC | Impossible (type info lost) | Not needed (`byte[]` fixed) |
| Deserialization | Double (Host + Worker) | Single (Worker only) |
| Confluent.Kafka dependency | Pulled into user's app | Zero |
| Cross-language | C# only | Unified across all languages |
| Binary efficiency | JSON + Base64 | Protobuf (native bytes) |
| User control | Host decides deserialization | User chooses their own deserializer |

In short: **`KafkaEventData<T>` was designed for the in-process model where Host = User process. The Isolated Worker's process separation and cross-language requirements demand a fundamentally different design — which is `KafkaRecord`.**

### Why Protobuf Instead of JSON

```
JSON path:     IKafkaEventData → JSON → Base64(key) + Base64(value) → ModelBindingData
Protobuf path: IKafkaEventData → Protobuf(native bytes) → ModelBindingData
```

| Consideration | JSON | Protobuf |
|---|---|---|
| **Binary data** | Base64 encoding required (33% size increase) | Native `bytes` field (zero overhead) |
| **Size** | Larger | Smaller |
| **Parse speed** | Slower | Faster |
| **Type safety** | No schema | Schema defined via `.proto` |
| **Cross-language** | Available in all languages | Available in all languages (protobuf libraries) |

Customer @KristofVDV raising the Base64 overhead issue was the direct reason for adopting Protobuf.

---

## 3. Architecture — Data Flow

### Azure Functions Isolated Worker Model

In the Isolated Worker model, the Host process and the Worker process are separate:

```
┌──────────────────────────────────────────────────────┐
│                    HOST PROCESS                       │
│                                                      │
│  Kafka Broker ──→ librdkafka ──→ ConsumeResult<K,V> │
│                                         │            │
│                         KafkaRecordProtobufSerializer│
│                         IKafkaEventData → Protobuf   │
│                                         │            │
│                         ModelBindingData             │
│                         source: "AzureKafkaRecord"   │
│                         content_type: "application/  │
│                                       x-protobuf"    │
│                         content: [protobuf bytes]    │
└────────────────────────────┬─────────────────────────┘
                             │ gRPC
┌────────────────────────────┴─────────────────────────┐
│                   WORKER PROCESS                      │
│                                                      │
│  KafkaRecordConverter                                │
│  [SupportsDeferredBinding]                           │
│    ModelBindingData → Protobuf parse → KafkaRecord   │
│                                         │            │
│  User Function                                       │
│  public void Run(                                    │
│    [KafkaTrigger(...)] KafkaRecord record            │
│  )                                                   │
│  {                                                   │
│    record.Topic, record.Partition, record.Offset     │
│    record.Key (byte[]), record.Value (byte[])        │
│    record.Headers, record.Timestamp                  │
│    record.LeaderEpoch                                │
│  }                                                   │
└──────────────────────────────────────────────────────┘
```

### What Is ModelBindingData?

`ModelBindingData` is a generic container for passing data between the Azure Functions Host and Worker processes.

```
ModelBindingData {
    version:      "1.0"
    source:       "AzureKafkaRecord"       ← binding source identifier
    content:      <binary protobuf bytes>  ← serialized KafkaRecordProto
    content_type: "application/x-protobuf"
}
```

The worker-side `KafkaRecordConverter` checks the `source` field to determine that this is a `KafkaRecord` binding, then deserializes `content` as Protobuf.

### Deferred Binding Pattern

`[SupportsDeferredBinding]` is a pattern where the Host does not fully parse the data; instead, the Worker performs lazy binding. EventHubs and ServiceBus extensions use the same pattern.

Normal flow:
```
Host: ConsumeResult → type conversion (string, etc.) → gRPC → Worker: pass to user
```

Deferred binding flow:
```
Host: ConsumeResult → ModelBindingData (raw) → gRPC → Worker: Converter parses → KafkaRecord → user
```

This allows the Worker to deserialize using its own type system, and the Host does not need to know about Worker types.

---

## 4. Protobuf Schema Details

```protobuf
message KafkaRecordProto {
    string topic = 1;              // Topic name
    int32 partition = 2;           // Partition number
    int64 offset = 3;              // Offset
    optional bytes key = 4;        // Key (nullable)
    optional bytes value = 5;      // Value (nullable)
    KafkaTimestampProto timestamp = 6;
    repeated KafkaHeaderProto headers = 7;
    optional int32 leader_epoch = 8;  // Broker leader epoch (nullable)
    reserved 9 to 15;             // Reserved for future expansion
}

message KafkaTimestampProto {
    int64 unix_timestamp_ms = 1;  // Unix milliseconds
    int32 type = 2;               // 0=NotAvailable, 1=CreateTime, 2=LogAppendTime
}

message KafkaHeaderProto {
    string key = 1;               // Header key
    optional bytes value = 2;     // Header value (nullable)
}
```

Design points:
- **`optional bytes key/value`**: In Kafka, key/value can be null. `optional` represents nullability
- **`repeated headers`**: Zero or more headers. Multiple entries with the same key are valid (per Kafka spec)
- **`reserved 9 to 15`**: Reserved for future fields without breaking compatibility
- **TimestampType enum**: Distinguishes `CreateTime` (set by producer) from `LogAppendTime` (set by broker)

---

## 5. Coexistence with Existing Paths

### Two Parallel Paths

The host extension has **two independent serialization paths**:

```
Path 1 (existing): IKafkaEventData → ParameterBindingData
  source: "AzureKafkaConsumeResult"  (JSON)
  → For existing string/byte[]/KafkaEventData<T> bindings

Path 2 (new):      IKafkaEventData → ParameterBindingData
  source: "AzureKafkaRecord"  (Protobuf)
  → For new KafkaRecord bindings
```

The worker-side `KafkaRecordConverter` only activates when `source == "AzureKafkaRecord"`. The existing path is completely untouched.

### Non-Breaking Guarantee

| Change | Impact on Existing Users |
|---|---|
| New `KafkaRecord` type | Additive — only activates when user opts in via function signature |
| New Protobuf path | New source identifier → invisible to existing converters |
| New `KafkaRecordConverter` | Only activates for `KafkaRecord` parameter type |
| Existing `string`, `byte[]`, `KafkaEventData<T>` | Completely unchanged |

---

## 6. Cross-Language Support

### Implementation Pattern per Language

| Language | Host Side | Worker / Extension Side | Mechanism |
|---|---|---|---|
| **.NET isolated** | Kafka 4.3.1 (Protobuf serialize) | Worker.Extensions.Kafka 4.2.0 (`KafkaRecordConverter`) | `IInputConverter` + `SupportsDeferredBinding` |
| **Java** | Same | java-library: `KafkaRecord` POJO + java-worker: `KafkaRecordProtoDeserializer` | `RpcModelBindingDataSource` dispatch |
| **Node.js** | Same | `@azure/functions-extensions-kafka`: `KafkaRecordFactory` | `ResourceFactoryResolver` pattern |
| **Python** | Same | `azurefunctions-extensions-bindings-kafka`: `KafkaRecordConverter` | `InConverter` + `SdkType` pattern |
| **PowerShell** | Same | No changes needed | Metadata already accessible via `$TriggerMetadata` |

All languages follow the same contract:
1. Check `ModelBindingData.source == "AzureKafkaRecord"`
2. Deserialize `ModelBindingData.content` as Protobuf
3. Map to the language-native `KafkaRecord` type

### Relationship with Extension Bundles

- The host-side Kafka extension 4.3.1 must be **included in the Extension Bundle** for non-.NET languages to use the `KafkaRecord` Protobuf path
- The current bundle pins Kafka at 4.2.0 → KafkaRecord is not yet available in bundles
- The bundle will be updated to include Kafka 4.3.1 after language-side extension/library PRs have been merged and released

---

## 7. User Experience — Usage Examples

### .NET Isolated Worker

```csharp
// Single record
[Function("KafkaTrigger")]
public void Run(
    [KafkaTrigger("BrokerList", "my-topic",
     ConsumerGroup = "$Default")] KafkaRecord record)
{
    var topic = record.Topic;
    var partition = record.Partition;
    var offset = record.Offset;
    var key = record.Key;           // byte[] or null
    var value = record.Value;       // byte[]
    var timestamp = record.Timestamp.DateTimeOffset;
    var leaderEpoch = record.LeaderEpoch;  // int? or null

    foreach (var header in record.Headers)
    {
        var headerKey = header.Key;
        var headerValue = header.GetValueAsString(); // convenience method
    }
}

// Batch
[Function("KafkaBatchTrigger")]
public void Run(
    [KafkaTrigger("BrokerList", "my-topic",
     ConsumerGroup = "$Default",
     IsBatched = true)] KafkaRecord[] records)
{
    foreach (var record in records)
    {
        // Full metadata access for each record
    }
}
```

### Python

```python
import azurefunctions.extensions.bindings.kafka as kafka

@app.kafka_trigger(...)
def my_function(record: kafka.KafkaRecord):
    topic = record.topic
    partition = record.partition
    key = record.key        # bytes or None
    value = record.value    # bytes
    for header in record.headers:
        print(f"{header.key}: {header.get_value_as_string()}")
```

### Node.js

```typescript
import '@azure/functions-extensions-kafka';
import type { KafkaRecord } from '@azure/functions-extensions-kafka';

app.kafka('kafkaTrigger', {
    ...,
    handler: async (record: KafkaRecord, context) => {
        context.log(`Topic: ${record.topic}, Offset: ${record.offset}`);
    }
});
```

---

## 8. FAQ — Anticipated Review Questions

| Question | Answer |
|---|---|
| **Why not pass `ConsumeResult` directly?** | The Worker process is separate from the Host process. `Confluent.Kafka.ConsumeResult` is not serializable and lacks a factory method to reconstruct from serialized bytes (unlike Azure SDK types which use AMQP). Custom serialization is required. |
| **Why Protobuf instead of JSON?** | Kafka key/value are raw bytes. JSON requires Base64 encoding, adding 33% size overhead and degrading performance. Customer feedback specifically raised this concern. |
| **Why no generics?** | Making Key/Value `byte[]` eliminates any dependency on `Confluent.Kafka`. Users choose their own deserializer. This also enables a unified cross-language design. |
| **Does this affect existing users?** | Zero impact. A new source identifier `AzureKafkaRecord` creates a separate path. Existing `string`/`byte[]`/`KafkaEventData<T>` paths are completely untouched. |
| **Are Host runtime changes needed?** | No. `ModelBindingData` / `ParameterBindingData` is existing infrastructure. Only extension package updates are needed. |
| **What about Extension Bundles?** | The bundle determines the host extension version. Until the bundle includes Kafka 4.3.1, non-.NET users must either wait for the bundle update or explicitly install the extension without using bundles. |
| **What about PowerShell?** | PowerShell can already access metadata via `$TriggerMetadata`, so a dedicated `KafkaRecord` type is not needed. |

---

## 9. Package and Version Summary

| Package | Version | Contains |
|---|---|---|
| `Microsoft.Azure.WebJobs.Extensions.Kafka` | 4.3.1 | Host-side: `KafkaRecordProtobufSerializer`, Protobuf schema, `ParameterBindingData` path |
| `Microsoft.Azure.Functions.Worker.Extensions.Kafka` | 4.2.0 | .NET isolated worker: `KafkaRecord`, `KafkaRecordConverter`, Protobuf deserialization |
| Extension Bundle | Currently 4.2.0 (pinned) | Will be updated to 4.3.1 after cross-language support is complete |

### Tracking

- Parent issue: [Azure/azure-functions-kafka-extension#612](https://github.com/Azure/azure-functions-kafka-extension/issues/612)
- .NET isolated PR: [Azure/azure-functions-dotnet-worker#3356](https://github.com/Azure/azure-functions-dotnet-worker/pull/3356) (merged)
- Java library PR: [Azure/azure-functions-java-library#236](https://github.com/Azure/azure-functions-java-library/pull/236)
- Java worker PR: [Azure/azure-functions-java-worker#869](https://github.com/Azure/azure-functions-java-worker/pull/869)
- Node.js PR: [Azure/azure-functions-nodejs-extensions#104](https://github.com/Azure/azure-functions-nodejs-extensions/pull/104)
- Python PR: [Azure/azure-functions-python-extensions#156](https://github.com/Azure/azure-functions-python-extensions/pull/156)
