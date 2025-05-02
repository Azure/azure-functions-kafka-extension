import { app, InvocationContext } from "@azure/functions";

export async function kafkaTrigger(
  event: any,
  context: InvocationContext
): Promise<void> {
  context.log("Event: " + JSON.stringify(event, null, 2));
}

// eventhub
app.generic("Kafkatrigger", {
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
  },
  handler: kafkaTrigger,
  retry: {
    strategy: "fixedDelay", // "exponentialBackoff" | "fixedDelay"
    maxRetryCount: 3,
    delayInterval: 300,
  },
});

// eventhub with exponentialBackoff
// app.generic("Kafkatrigger", {
//   trigger: {
//     type: "kafkaTrigger",
//     direction: "in",
//     name: "event",
//     topic: "topic",
//     brokerList: "%BrokerList%",
//     username: "$ConnectionString",
//     password: "EventHubConnectionString",
//     consumerGroup: "$Default",
//     protocol: "saslSsl",
//     authenticationMode: "plain",
//     dataType: "string",
//   },
//   handler: kafkaTrigger,
//   retry: {
//     strategy: "exponentialBackoff",
//     maxRetryCount: 3,
//     minimumInterval: 300,
//     maximumInterval: 10000,
//   },
// });

// confluent
// app.generic("Kafkatrigger", {
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
//     dataType: "string"
//   },
//   handler: kafkaTrigger,
// });
