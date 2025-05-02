import { app, InvocationContext } from "@azure/functions";

export async function kafkaTriggerWithRetry(
  event: any,
  context: InvocationContext
): Promise<void> {
  context.log("Event: " + JSON.stringify(event, null, 2));
}

// eventhub
app.generic("kafkaTriggerWithRetry", {
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
  handler: kafkaTriggerWithRetry,
  retry: {
    strategy: "fixedDelay", // "exponentialBackoff" | "fixedDelay"
    maxRetryCount: 3,
    delayInterval: 300,
  },
});

// eventhub with exponentialBackoff
// app.generic("kafkaTriggerWithRetry", {
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
//   handler: kafkaTriggerWithRetry,
//   retry: {
//     strategy: "exponentialBackoff",
//     maxRetryCount: 3,
//     minimumInterval: 300,
//     maximumInterval: 10000,
//   },
// });

// confluent
// app.generic("kafkaTriggerWithRetry", {
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
//     retry: {
//       strategy: "fixedDelay", // "exponentialBackoff" | "fixedDelay"
//       maxRetryCount: 3,
//       delayInterval: 300,
//     },
//   },
//   handler: kafkaTriggerWithRetry,
// });
