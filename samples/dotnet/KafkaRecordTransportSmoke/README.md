# KafkaRecord Transport Smoke Sample

This is a temporary host-side smoke sample for KafkaRecord deferred-binding transport validation.

It intentionally uses an in-process .NET function parameter of `ParameterBindingData` to validate that the Kafka extension:

- Starts under `func start`
- Creates the Kafka consumer with a `byte[]` value deserializer for deferred binding
- Emits `ParameterBindingData` with `Source = "AzureKafkaRecord"`
- Emits `ContentType = "application/x-protobuf"`
- Produces a parseable KafkaRecord Protobuf payload

The customer-facing `KafkaRecord` type belongs to the .NET isolated worker package. After the Kafka extension and .NET isolated Kafka extension releases are complete, this temporary sample should be replaced by a formal `samples/dotnet-isolated` KafkaRecord sample.

## Local run

1. Copy `local.settings.sample.json` to `local.settings.json`.
2. Start a local Kafka broker on `localhost:9092` with topic `test-topic`, or update `KafkaRecordSmokeTopic` in `local.settings.json`.
3. Run:

```bash
func start
```

4. Produce a test message:

```bash
curl "http://localhost:7071/api/produce?message=hello-kafkarecord-transport"
```

5. Check status:

```bash
curl "http://localhost:7071/api/status?message=hello-kafkarecord-transport"
```
