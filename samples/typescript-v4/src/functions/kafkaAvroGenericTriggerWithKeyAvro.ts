import { app, InvocationContext } from "@azure/functions";

export async function kafkaAvroGenericTriggerWithKeyAvro(
  event: any,
  context: InvocationContext
): Promise<void> {
  context.log("Processed kafka event: ", event);
  context.log(
    `Message ID: ${event.id}, amount: ${event.amount}, type: ${event.type}`
  );
  if (context.triggerMetadata?.key !== undefined) {
    let key = context.triggerMetadata?.key as { id: string; timestamp: string };
    context.log(`Key ID: ${key.id}, timestamp: ${key.timestamp}`);
  }
}

// confluent
app.generic("kafkaAvroGenericTriggerWithKeyAvro", {
  trigger: {
    type: "kafkaTrigger",
    direction: "in",
    name: "event",
    protocol: "SASLSSL",
    username: "ConfluentCloudUsername",
    password: "ConfluentCloudPassword",
    dataType: "string",
    topic: "topic",
    authenticationMode: "PLAIN",
    avroSchema:
      '{"type":"record","name":"Payment","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},{"name":"amount","type":"double"},{"name":"type","type":"string"}]}',
    keyAvroSchema:
      '{"type":"record","name":"PaymentKey","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},{"name":"timestamp","type":"string"}]}',
    consumerGroup: "$Default",
    brokerList: "%BrokerList%",
  },
  handler: kafkaAvroGenericTriggerWithKeyAvro,
});

// eventhub
// app.generic("kafkaAvroGenericTriggerWithKeyAvro", {
//   trigger: {
//     type: "kakfaTrigger",
//     direction: "in",
//     name: "event",
//     protocol: "SASLSSL",
//     password: "EventHubConnectionString",
//     dataType: "string",
//     topic: "topic",
//     authenticationMode: "PLAIN",
//     avroSchema:
//       '{"type":"record","name":"Payment","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},{"name":"amount","type":"double"},{"name":"type","type":"string"}]}',
//     keyAvroSchema:
//      '{"type":"record","name":"PaymentKey","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},''{"name":"timestamp","type":"string"}]}',
//     consumerGroup: "$Default",
//     username: "$ConnectionString",
//     brokerList: "%BrokerList%",
//   },
//   handler: kafkaAvroGenericTriggerWithKeyAvro,
// });
