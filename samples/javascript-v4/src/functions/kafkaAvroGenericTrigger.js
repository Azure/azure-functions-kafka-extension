const { app } = require("@azure/functions");

async function kafkaAvroGenericTrigger(event, context) {
  context.log("Processed kafka event: ", event);
  if (context.triggerMetadata?.key !== undefined) {
    context.log("message key: ", context.triggerMetadata?.key);
  }
}

// confluent
app.generic("kafkaAvroGenericTrigger", {
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
    consumerGroup: "$Default",
    brokerList: "%BrokerList%",
  },
  handler: kafkaAvroGenericTrigger,
});

// eventhub
// app.generic("kafkaAvroGenericTrigger", {
//   trigger: {
//     type: "kafkaTrigger",
//     direction: "in",
//     name: "event",
//     protocol: "SASLSSL",
//     password: "EventHubConnectionString",
//     dataType: "string",
//     topic: "topic",
//     authenticationMode: "PLAIN",
//     avroSchema:
//       '{"type":"record","name":"Payment","namespace":"io.confluent.examples.clients.basicavro","fields":[{"name":"id","type":"string"},{"name":"amount","type":"double"},{"name":"type","type":"string"}]}',
//     consumerGroup: "$Default",
//     username: "$ConnectionString",
//     brokerList: "%BrokerList%",
//   },
//   handler: kafkaAvroGenericTrigger,
// });
