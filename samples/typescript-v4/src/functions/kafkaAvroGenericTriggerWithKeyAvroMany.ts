import { app, InvocationContext } from "@azure/functions";

export async function kafkaAvroGenericTriggerWithKeyAvroMany(
  events: any,
  context: InvocationContext
): Promise<void> {
  events.forEach((event, index) => {
    context.log("Processed kafka event: ", event);
    context.log(
      `Message ID: ${event.id}, amount: ${event.amount}, type: ${event.type}`
    );

    const key = context.triggerMetadata?.keyArray?.[index];
    if (key !== undefined) {
      context.log(`Key ID: ${key.id}, timestamp: ${key.timestamp}`);
    }
  });
}

// confluent
app.generic("kafkaAvroGenericTriggerWithKeyAvroMany", {
  trigger: {
    type: "kafkaTrigger",
    direction: "in",
    name: "events",
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
  handler: kafkaAvroGenericTriggerWithKeyAvroMany,
});

// eventhub
// app.generic("kafkaAvroGenericTriggerWithKeyAvroMany", {
//   trigger: {
//     type: "kakfaTrigger",
//     direction: "in",
//     name: "events",
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
//   handler: kafkaAvroGenericTriggerWithKeyAvroMany,
// });
