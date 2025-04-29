import { app, InvocationContext } from "@azure/functions";

export async function KafkatriggerWithKeyValueDataType(
  event: any,
  context: InvocationContext
): Promise<void> {
  context.log("Event Offset: " + context.triggerMetadata?.offset);
  context.log("Event Partition: " + context.triggerMetadata?.partition);
  context.log("Event Topic: " + context.triggerMetadata?.topic);
  context.log("Event Timestamp: " + context.triggerMetadata?.timestamp);
  context.log("Event Value (as string): " + event.toString("utf-8"));
  context.log(
    `Event Key (as string) : ${context.triggerMetadata?.key?.toString("utf-8")}`
  );
}

// eventhub
app.generic("KafkatriggerWithKeyValueDataType", {
  trigger: {
    type: "kafkaTrigger",
    direction: "in",
    name: "event",
    topic: "topic",
    brokerList: "%BrokerList%",
    username: "$ConnectionString",
    password: "EventHubConnectionString",
    consumerGroup: "$Default",
    protocol: "saslSsl",
    authenticationMode: "plain",
    dataType: "binary",
    keyDataType: "binary",
  },
  handler: KafkatriggerWithKeyValueDataType,
});

// confluent
// app.generic("KafkatriggerWithKeyValueDataType", {
//   trigger: {
//     type: "KafkatriggerWithKeyValueDataType",
//     direction: "in",
//     name: "event",
//     topic: "topic",
//     brokerList: "%BrokerList%",
//     username: "%ConfluentCloudUserName%",
//     password: "%ConfluentCloudPassword%",
//     consumerGroup: "$Default",
//     protocol: "saslSsl",
//     authenticationMode: "plain",
//     dataType: "binary"
//     keyDataType: "binary"
//   },
//   handler: KafkatriggerWithKeyValueDataType,
// });
