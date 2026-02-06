# Consumer Sharing パターン仕様書

## Overview

Azure Functions Kafka Extension で、同一設定の複数 Kafka Trigger が librdkafka Consumer を共有するパターンを実装する。これにより、多数の Kafka Trigger を持つ Function App のスレッド数を大幅に削減する。

## Background

### 現状の問題

| 項目 | 現状 |
|------|------|
| Consumer 生成 | 1 Trigger = 1 Consumer |
| スレッド数 | 1 Consumer = ~25 threads (librdkafka 内部スレッド) |
| 例：31 triggers | 31 × 25 = ~775 threads |

### 根本原因

librdkafka は Consumer/Producer インスタンスごとに固定数の内部スレッドを生成する：
- Main thread (1)
- Broker connection threads (~3 per broker)
- Other internal threads (~10)

この設計は librdkafka のアーキテクチャに由来し、Confluent.Kafka (.NET wrapper) では変更不可。

### 既存コードの TODO

```csharp
// KafkaTriggerAttributeBindingProvider.cs:71
Task<IListener> listenerCreator(ListenerFactoryContext factoryContext, bool singleDispatch)
{
    // TODO: reuse connections if they match with others in same function app
    var listener = new KafkaListener<TKey, TValue>(...);
```

---

## Proposed Solution: Consumer Sharing

### 概念図

```
現状:
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

改善後 (Consumer Sharing):
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

同一 Consumer を共有できる条件 (全て一致する必要あり):

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

新規コンポーネント `IKafkaConsumerPool` を導入：

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

単一 Consumer から複数 Trigger へのメッセージ分配：

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

## 変更が必要なファイル

| ファイル | 変更内容 | 影響度 |
|----------|----------|--------|
| `KafkaTriggerAttributeBindingProvider.cs` | Consumer Pool 使用に変更 | High |
| `KafkaListener.cs` | SharedConsumer 使用に変更 | High |
| **新規** `IKafkaConsumerPool.cs` | インターフェース定義 | - |
| **新規** `KafkaConsumerPool.cs` | Pool 実装 | - |
| **新規** `SharedConsumer.cs` | 共有 Consumer ラッパー | - |
| **新規** `ConsumerSharingKey.cs` | キー定義 | - |
| `KafkaWebJobsStartup.cs` | DI 登録 | Low |
| `FunctionExecutorBase.cs` | Commit strategy 調整 | Medium |

---

## 考慮事項

### 1. Offset Commit Strategy

**問題**: 複数 Trigger が同一 topic を共有する場合、offset commit の競合が発生しうる。

**解決案**:
- Per-partition offset tracking を各 handler で独立管理
- Commit は最小 offset のみ実行 (at-least-once semantics 維持)

### 2. Consumer Group Semantics

**問題**: 同一 consumer group で複数 topic を subscribe すると、partition assignment が変わる可能性。

**解決案**:
- Topic ごとに異なる internal consumer を使用する選択肢を提供
- または、sharing は同一 topic の trigger 間のみに限定

### 3. Lifecycle Management

**問題**: Trigger の start/stop タイミングが異なる場合の Consumer lifecycle。

**解決案**:
- Reference counting による管理
- 最後の参照が解放されるまで Consumer を維持
- Drain mode 対応

### 4. Error Handling

**問題**: 共有 Consumer でエラー発生時、全 trigger に影響。

**解決案**:
- Per-handler error isolation
- Consumer 再作成時の自動復旧

### 5. Metrics & Monitoring

**問題**: 共有時のメトリクス集約方法。

**解決案**:
- Per-trigger メトリクスは維持
- Consumer-level メトリクスは共有

### 6. Breaking Changes

**Opt-in approach** を推奨：
- 新規設定 `KafkaOptions.EnableConsumerSharing = false` (デフォルト)
- 既存動作との互換性維持
- Phase 2 でデフォルト有効化を検討

---

## Implementation Phases

### Phase 1: Foundation (MVP)
- [ ] ConsumerSharingKey 実装
- [ ] IKafkaConsumerPool インターフェース定義
- [ ] KafkaConsumerPool 基本実装
- [ ] SharedConsumer 基本実装
- [ ] 同一 topic/consumerGroup での sharing のみ対応

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

**スレッド数削減率**: ~97%

---

## Related Issues

- librdkafka FAQ: [Number of Internal Threads](https://github.com/confluentinc/librdkafka/wiki/FAQ#number-of-internal-threads)
- librdkafka Threading Model: [INTRODUCTION.md](https://github.com/confluentinc/librdkafka/blob/master/INTRODUCTION.md#threads-and-callbacks)
- Confluent.Kafka Issue #2232: [Too many threads](https://github.com/confluentinc/confluent-kafka-dotnet/issues/2232)

---

## Open Questions

1. **Sharing Scope**: 同一 topic 限定 vs. 同一 broker/consumerGroup で複数 topic を許可？
2. **Default Behavior**: Opt-in (安全) vs. Opt-out (既存ユーザーへの影響)?
3. **Scale Out Strategy**: Consumer sharing 時の KEDA スケーリングへの影響は？

---

## References

- Current implementation: [KafkaListener.cs](azure-functions-kafka-extension/src/Microsoft.Azure.WebJobs.Extensions.Kafka/Listeners/KafkaListener.cs)
- Binding provider: [KafkaTriggerAttributeBindingProvider.cs](azure-functions-kafka-extension/src/Microsoft.Azure.WebJobs.Extensions.Kafka/Trigger/KafkaTriggerAttributeBindingProvider.cs)
- Investigation report: [REPORT-scalecontroller-thread-investigation-20260205.md](docs/investigation/REPORT-scalecontroller-thread-investigation-20260205.md)
