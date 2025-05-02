const { app } = require("@azure/functions");

async function kafkaTriggerWithRetry(event, context) {
  context.log("Processed kafka event: ", event);
}

// confluent
app.generic("kafkaTriggerWithRetry", {
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
    brokerList: "%BrokerList%",
  },
  handler: kafkaTriggerWithRetry,
  retry: {
    strategy: "fixedDelay", // "exponentialBackoff" | "fixedDelay"
    maxRetryCount: 3,
    delayInterval: 300,
  },
});

// confluent
// app.generic("kafkaTriggerWithRetry", {
//     trigger: {
//       type: "kafkaTrigger",
//       direction: "in",
//       name: "event",
//       protocol: "SASLSSL",
//       username: "ConfluentCloudUsername",
//       password: "ConfluentCloudPassword",
//       dataType: "string",
//       topic: "topic",
//       authenticationMode: "PLAIN",
//       brokerList: "%BrokerList%",
//     },
//     handler: kafkaTriggerWithRetry,
//     retry: {
//         strategy: "exponentialBackoff",
//         maxRetryCount: 3,
//         minimumInterval: 300,
//         maximumInterval: 10000,
//     },
//   });

// eventhub
// app.generic("kafkaTriggerWithRetry", {
//   trigger: {
//     type: "kakfaTrigger",
//     direction: "in",
//     name: "event",
//     protocol: "SASLSSL",
//     password: "EventHubConnectionString",
//     dataType: "string",
//     topic: "topic",
//     authenticationMode: "PLAIN",
//     consumerGroup: "$Default",
//     username: "$ConnectionString",
//     brokerList: "%BrokerList%",
//   },
//   handler: kafkaTriggerWithRetry,
//   retry: {
//       strategy: "fixedDelay", // "exponentialBackoff" | "fixedDelay"
//       maxRetryCount: 3,
//       delayInterval: 300,
//   },
// });
