# Consumer Sharing Pattern Specification

## Overview

Implement a Consumer Sharing pattern in Azure Functions Kafka Extension where multiple Kafka Triggers with matching configurations share a single librdkafka Consumer instance. This significantly reduces the thread count for Function Apps with many Kafka Triggers.

## Background

### Current Problem

| Item | Current State |
|------|---------------|
| Consumer creation | 1 Trigger = 1 Consumer |
| Thread count | 1 Consumer = ~25 threads (librdkafka internal threads) |
| Example: 31 triggers | 31 × 25 = ~775 threads |

### Root Cause

librdkafka creates a fixed number of internal threads per Consumer/Producer instance:
- Main thread (1)
- Broker connection threads (~3 per broker)
- Other internal threads (~10)

This design is inherent to librdkafka's architecture and cannot be changed at the Confluent.Kafka (.NET wrapper) level.

### Existing TODO in Code

```csharp
// KafkaTriggerAttributeBindingProvider.cs:71
Task<IListener> listenerCreator(ListenerFactoryContext factoryContext, bool singleDispatch)
{
    // TODO: reuse connections if they match with others in same function app
    var listener = new KafkaListener<TKey, TValue>(...);
```

---

## Proposed Solution: Consumer Sharing

### Conceptual Diagram

```
Current State:
┌─────────────────────────────────────────────────────────────┐
│ Function App                                                │
│                                                             │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐       │
│  │  Trigger 1  │   │  Trigger 2  │   │  Trigger N  │  ...  │
│  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘       │
│         │                 │                 │               │
│         ▼                 ▼                 ▼               │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐       │
│  │ Consumer 1  │   │ Consumer 2  │   │ Consumer N  │       │
│  │  ~25 thrds  │   │  ~25 thrds  │   │  ~25 thrds  │       │
│  └─────────────┘   └─────────────┘   └─────────────┘       │
│                                                             │
│  Total: N × 25 threads                                      │
└─────────────────────────────────────────────────────────────┘

After Improvement (Consumer Sharing):
┌─────────────────────────────────────────────────────────────┐
│ Function App                                                │
│                                                             │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐       │
│  │  Trigger 1  │   │  Trigger 2  │   │  Trigger N  │  ...  │
│  │  (topic-A)  │   │  (topic-B)  │   │  (topic-A)  │       │
│  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘       │
│         │                 │                 │               │
│         │                 │                 │               │
│         ▼                 ▼                 │               │
│  ┌─────────────┐   ┌─────────────┐         │               │
│  │  Consumer 1 │   │  Consumer 2 │◄────────┘               │
│  │  (broker A) │   │  (broker A) │                         │
│  │  ~25 thrds  │   │  ~25 thrds  │                         │
│  └─────────────┘   └─────────────┘                         │
│                                                             │
│  Total: U × 25 threads (U = unique broker/consumerGroup)    │
└─────────────────────────────────────────────────────────────┘
```

---

## Design Details

### Consumer Sharing Key

Conditions for sharing a Consumer (all must match):

```csharp
public class ConsumerSharingKey : IEquatable<ConsumerSharingKey>
{
    public string BrokerList { get; }
    public string ConsumerGroup { get; }
    public SecurityProtocol SecurityProtocol { get; }
    public SaslMechanism? SaslMechanism { get; }
    public string SaslUsername { get; }
    // Note: Password hash only, not actual password
    public string SslCaLocation { get; }
    // ... other security settings
}
```

### Consumer Pool Manager

Introduce a new component `IKafkaConsumerPool`:

```csharp
public interface IKafkaConsumerPool
{
    /// <summary>
    /// Get or create a shared consumer for the given key.
    /// </summary>
    ISharedConsumer<TKey, TValue> GetOrCreateConsumer<TKey, TValue>(
        ConsumerSharingKey key,
        ConsumerConfig config,
        IDeserializer<TKey> keyDeserializer,
        IDeserializer<TValue> valueDeserializer);

    /// <summary>
    /// Release a reference to a shared consumer.
    /// Consumer is disposed when reference count reaches 0.
    /// </summary>
    void ReleaseConsumer(ConsumerSharingKey key);
}
```

### Shared Consumer

```csharp
public interface ISharedConsumer<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Subscribe a listener to receive messages for a topic.
    /// Multiple listeners can subscribe to the same topic.
    /// </summary>
    void Subscribe(string topic, IMessageHandler<TKey, TValue> handler);

    /// <summary>
    /// Unsubscribe a listener from a topic.
    /// </summary>
    void Unsubscribe(string topic, IMessageHandler<TKey, TValue> handler);
}
```

### Message Dispatcher

Dispatching messages from a single Consumer to multiple Triggers:

```csharp
internal class SharedConsumerMessageDispatcher<TKey, TValue>
{
    // topic -> handlers mapping
    private readonly ConcurrentDictionary<string, List<IMessageHandler<TKey, TValue>>> _handlers;

    public void DispatchMessage(ConsumeResult<TKey, TValue> result)
    {
        if (_handlers.TryGetValue(result.Topic, out var handlers))
        {
            foreach (var handler in handlers)
            {
                handler.HandleMessage(result);
            }
        }
    }
}
```

---

## Files to Change

| File | Change Description | Impact |
|------|-------------------|--------|
| `KafkaTriggerAttributeBindingProvider.cs` | Use Consumer Pool | High |
| `KafkaListener.cs` | Use SharedConsumer | High |
| **New** `IKafkaConsumerPool.cs` | Interface definition | - |
| **New** `KafkaConsumerPool.cs` | Pool implementation | - |
| **New** `SharedConsumer.cs` | Shared Consumer wrapper | - |
| **New** `ConsumerSharingKey.cs` | Key definition | - |
| `KafkaWebJobsStartup.cs` | DI registration | Low |
| `FunctionExecutorBase.cs` | Commit strategy adjustment | Medium |

---

## Considerations

### 1. Offset Commit Strategy

**Problem**: When multiple Triggers share the same topic, offset commit conflicts may occur.

**Solution**:
- Manage per-partition offset tracking independently for each handler
- Commit only the minimum offset (maintaining at-least-once semantics)

### 2. Consumer Group Semantics

**Problem**: Subscribing to multiple topics with the same consumer group may change partition assignment.

**Solution**:
- Provide option to use different internal consumers per topic
- Or limit sharing to triggers on the same topic only

### 3. Lifecycle Management

**Problem**: Consumer lifecycle when Triggers have different start/stop timing.

**Solution**:
- Manage via reference counting
- Maintain Consumer until last reference is released
- Support drain mode

### 4. Error Handling

**Problem**: Errors in shared Consumer affect all triggers.

**Solution**:
- Per-handler error isolation
- Automatic recovery on Consumer recreation

### 5. Metrics & Monitoring

**Problem**: How to aggregate metrics when sharing.

**Solution**:
- Maintain per-trigger metrics
- Share Consumer-level metrics

### 6. Breaking Changes

**Opt-in approach** recommended:
- New setting `KafkaOptions.EnableConsumerSharing = false` (default)
- Maintain compatibility with existing behavior
- Consider enabling by default in Phase 2

---

## Implementation Phases

### Phase 1: Foundation (MVP)
- [ ] Implement ConsumerSharingKey
- [ ] Define IKafkaConsumerPool interface
- [ ] Implement basic KafkaConsumerPool
- [ ] Implement basic SharedConsumer
- [ ] Support sharing only for same topic/consumerGroup

### Phase 2: Production Ready
- [ ] Comprehensive error handling
- [ ] Metrics integration
- [ ] Unit tests & Integration tests
- [ ] Documentation

### Phase 3: Advanced Features
- [ ] Cross-topic sharing (optional opt-in)
- [ ] Dynamic scaling integration
- [ ] Admin API sharing (optional)

---

## Expected Impact

### Before (31 Kafka triggers, same broker/consumerGroup/topic)

| Metric | Value |
|--------|-------|
| Consumer instances | 31 |
| librdkafka threads | ~775 |
| Memory overhead | High |
| Connection count | 31 × brokers |

### After (Consumer Sharing enabled)

| Metric | Value |
|--------|-------|
| Consumer instances | 1 |
| librdkafka threads | ~25 |
| Memory overhead | Low |
| Connection count | 1 × brokers |

**Thread reduction rate**: ~97%

---

## Related Issues

- librdkafka FAQ: [Number of Internal Threads](https://github.com/confluentinc/librdkafka/wiki/FAQ#number-of-internal-threads)
- librdkafka Threading Model: [INTRODUCTION.md](https://github.com/confluentinc/librdkafka/blob/master/INTRODUCTION.md#threads-and-callbacks)
- Confluent.Kafka Issue #2232: [Too many threads](https://github.com/confluentinc/confluent-kafka-dotnet/issues/2232)

---

## Open Questions

1. **Sharing Scope**: Limit to same topic only vs. allow multiple topics with same broker/consumerGroup?
2. **Default Behavior**: Opt-in (safe) vs. Opt-out (impact on existing users)?
3. **Scale Out Strategy**: How does Consumer sharing affect KEDA scaling?

---

## References

- Current implementation: [KafkaListener.cs](azure-functions-kafka-extension/src/Microsoft.Azure.WebJobs.Extensions.Kafka/Listeners/KafkaListener.cs)
- Binding provider: [KafkaTriggerAttributeBindingProvider.cs](azure-functions-kafka-extension/src/Microsoft.Azure.WebJobs.Extensions.Kafka/Trigger/KafkaTriggerAttributeBindingProvider.cs)
- Investigation report: [REPORT-scalecontroller-thread-investigation-20260205.md](docs/investigation/REPORT-scalecontroller-thread-investigation-20260205.md)
