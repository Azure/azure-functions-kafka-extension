const { app } = require("@azure/functions");

async function kafkaTriggerMany(events, context) {
  for (const event of events) {
    context.log("Event Offset: " + event.Offset);
    context.log("Event Partition: " + event.Partition);
    context.log("Event Topic: " + event.Topic);
    context.log("Event Key: " + event.Key);
    context.log("Event Timestamp: " + event.Timestamp);
    context.log("Event Value (as string): " + event.Value);

    let event_obj = JSON.parse(event.Value);

    context.log("Event Value Object: ");
    context.log("   Value.registertime: ", event_obj.registertime.toString());
    context.log("   Value.userid: ", event_obj.userid);
    context.log("   Value.regionid: ", event_obj.regionid);
    context.log("   Value.gender: ", event_obj.gender);
  }
}

// eventhub
app.generic("kafkaTriggerMany", {
  trigger: {
    type: "kafkaTrigger",
    direction: "in",
    name: "events",
    topic: "topic",
    brokerList: "%BrokerList%",
    username: "$ConnectionString",
    password: "EventHubConnectionString",
    consumerGroup: "$Default",
    protocol: "saslSsl",
    authenticationMode: "plain",
    dataType: "string",
    cardinality: "MANY",
  },
  handler: kafkaTriggerMany,
});

// confluent
// app.generic("kafkaTriggerMany", {
//   trigger: {
//     type: "kafkaTrigger",
//     direction: "in",
//     name: "event",
//     topic: "topic",
//     brokerList: "%BrokerList%",
//     username: "%ConfluentCloudUserName%",
//     password: "%ConfluentCloudPassword%",
//     consumerGroup: "$Default",
//     protocol: "saslSsl",
//     authenticationMode: "plain",
//     dataType: "string",
//     cardinality: "MANY"
//   },
//   handler: kafkaTriggerMany,
// });
