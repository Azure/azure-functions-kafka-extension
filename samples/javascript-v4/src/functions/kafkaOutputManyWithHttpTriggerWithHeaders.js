const { app, output } = require("@azure/functions");

// Eventhub
const kafkaOutput = output.generic({
  type: "kafka",
  direction: "out",
  topic: "topic",
  brokerList: "%BrokerList%",
  username: "$ConnectionString",
  password: "EventHubConnectionString",
  protocol: "saslSsl",
  authenticationMode: "plain",
});

// Confluent
// const kafkaOutput = output.generic({
//     type: "kafka",
//     direction: "out",
//     topic: "topic",
//     brokerList: "%BrokerList%",
//     username: "ConfluentCloudUsername",
//     password: "ConfluentCloudPassword",
//     protocol: "saslSsl",
//     authenticationMode: "plain",
//});

export async function kafkaOutputWithHttp(request, context) {
  context.log(`Http function processed request for url "${request.url}"`);

  const queryName = request.query.get("name");
  const body = await request.text();
  const parsedbody = body ? JSON.parse(body) : {};
  parsedbody.name = parsedbody.name || "world";
  const name = queryName || parsedbody.name;
  const messages = [
    `Message one. Hello ${name}!`,
    `Message two. Hello ${name}!`,
  ];
  for (const message of messages) {
    let timestamp = new Date();
    let key = timestamp.getHours().toString();
    context.extraOutputs.set(
      kafkaOutput,
      `{ "Offset":364,"Partition":0,"Topic":"topic","Timestamp":"${timestamp}", "Value": "${message}", "Key":"${key}", "Headers": [{ "Key": "language", "Value": "typescript" }] }`
    );
    context.log(
      `Sending message to kafka: ${context.extraOutputs.get(kafkaOutput)}`
    );
  }

  return {
    body: `Messages sent to kafka topic.`,
    status: 200,
  };
}

const extraOutputs = [];
extraOutputs.push(kafkaOutput);

app.http("kafkaOutputWithHttp", {
  methods: ["GET", "POST"],
  authLevel: "anonymous",
  extraOutputs,
  handler: kafkaOutputWithHttp,
});
