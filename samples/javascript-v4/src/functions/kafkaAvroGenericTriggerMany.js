const { app } = require("@azure/functions");

export async function kafkaAvroGenericTriggerMany(events, context) {
  for (var event of events) {
    context.log("Processed kafka event: ", event);
  }
  if (context.triggerMetadata?.key !== undefined) {
    context.log("message key: ", context.triggerMetadata?.key);
  }
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
