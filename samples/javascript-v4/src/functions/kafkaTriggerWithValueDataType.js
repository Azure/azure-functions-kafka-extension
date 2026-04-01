const { app } = require("@azure/functions");

async function KafkatriggerWithValueDataType(event, context) {
  context.log("Event Offset: " + context.triggerMetadata?.offset);
  context.log("Event Partition: " + context.triggerMetadata?.partition);
  context.log("Event Topic: " + context.triggerMetadata?.topic);
  context.log("Event Timestamp: " + context.triggerMetadata?.timestamp);
  context.log("Event Value (as string): " + event.toString("utf-8"));
  context.log(`Event Key: ${context.triggerMetadata?.key}`);
}

// eventhub
app.generic("KafkatriggerWithValueDataType", {
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
  },
  handler: KafkatriggerWithValueDataType,
});

// confluent
// app.generic("KafkatriggerWithValueDataType", {
//   trigger: {
//     type: "KafkatriggerWithValueDataType",
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
//   },
//   handler: KafkatriggerWithValueDataType,
// });
