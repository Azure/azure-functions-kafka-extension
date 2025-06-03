const { app } = require("@azure/functions");

async function KafkatriggerWithKeyDataType(event, context) {
  context.log("Event Offset: " + event.Offset);
  context.log("Event Partition: " + event.Partition);
  context.log("Event Topic: " + event.Topic);
  context.log("Event Timestamp: " + event.Timestamp);
  context.log("Event Value (as string): " + event.Value);
  context.log(`Event Key (as string) : ${atob(event.Key)}`);
}

// eventhub
app.generic("KafkatriggerWithKeyDataType", {
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
    dataType: "string",
    keyDataType: "binary",
  },
  handler: KafkatriggerWithKeyDataType,
});

// confluent
// app.generic("KafkatriggerWithKeyDataType", {
//   trigger: {
//     type: "KafkatriggerWithKeyDataType",
//     direction: "in",
//     name: "event",
//     topic: "topic",
//     brokerList: "%BrokerList%",
//     username: "%ConfluentCloudUserName%",
//     password: "%ConfluentCloudPassword%",
//     consumerGroup: "$Default",
//     protocol: "saslSsl",
//     authenticationMode: "plain",
//     dataType: "string"
//     keyDataType: "binary"
//   },
//   handler: KafkatriggerWithKeyDataType,
// });
