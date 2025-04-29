import { app, InvocationContext } from "@azure/functions";

export async function kafkaAvroGenericTriggerMany(
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
      context.log("Message key: ", key);
    }
  });
}

// confluent
app.generic("kafkaAvroGenericTriggerMany", {
  trigger: {
    type: "kafkaTrigger",
    direction: "in",
    name: "events",
    protocol: "SASLSSL",
    password: "ConfluentCloudPassword",
    dataType: "string",
    topic: "topic",
    authenticationMode: "PLAIN",
    avroSchema:
      '{"type":"record","name":"Payment","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},{"name":"amount","type":"double"},{"name":"type","type":"string"}]}',
    cardinality: "MANY",
    consumerGroup: "$Default",
    username: "ConfluentCloudUsername",
    brokerList: "%BrokerList%",
  },
  handler: kafkaAvroGenericTriggerMany,
});

// eventhub
// app.generic("kafkaAvroGenericTriggerMany", {
//   trigger: {
//     type: "kafkaTrigger",
//     direction: "in",
//     name: "events",
//     protocol: "SASLSSL",
//     password: "EventHubConnectionString",
//     dataType: "string",
//     topic: "topic",
//     authenticationMode: "PLAIN",
//     avroSchema:
//       '{"type":"record","name":"Payment","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},{"name":"amount","type":"double"},{"name":"type","type":"string"}]}',
//     cardinality: "MANY",
//     consumerGroup: "$Default",
//     username: "$ConnectionString",
//     brokerList: "%BrokerList%",
//   },
//   handler: kafkaAvroGenericTriggerMany,
// });
