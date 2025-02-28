import { app, InvocationContext } from "@azure/functions";

export async function kafkaAvroGenericTriggerMany(
  events: unknown[],
  context: InvocationContext
): Promise<void> {
  for (var event of events as string[]) {
    context.log("Processed kafka event: ", event);
  }
  if (context.triggerMetadata?.key !== undefined) {
    context.log("message key: ", context.triggerMetadata?.key as string);
  }
}

// confluent
app.generic("kafkaAvroGenericTriggerMany", {
  trigger: {
    type: "kakfaTrigger",
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
//     cardinality: "MANY",
//     consumerGroup: "$Default",
//     username: "$ConnectionString",
//     brokerList: "%BrokerList%",
//   },
//   handler: kafkaAvroGenericTriggerMany,
// });
