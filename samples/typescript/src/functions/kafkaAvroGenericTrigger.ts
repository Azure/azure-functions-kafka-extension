import { app, InvocationContext } from "@azure/functions";

export async function kafkaAvroGenericTrigger(
  event: unknown,
  context: InvocationContext
): Promise<void> {
  context.log("Processed kafka event: ", event);
  if (context.triggerMetadata?.key !== undefined) {
    context.log("message key: ", btoa(context.triggerMetadata?.key as string));
  }
}

// confluent
app.generic("kafkaAvroGenericTrigger", {
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
    consumerGroup: "$Default",
    username: "ConfluentCloudUsername",
    brokerList: "%BrokerList%",
  },
  handler: kafkaAvroGenericTrigger,
});

// eventhub
// app.generic("kafkaAvroGenericTrigger", {
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
//     consumerGroup: "$Default",
//     username: "$ConnectionString",
//     brokerList: "%BrokerList%",
//   },
//   handler: kafkaAvroGenericTrigger,
// });
