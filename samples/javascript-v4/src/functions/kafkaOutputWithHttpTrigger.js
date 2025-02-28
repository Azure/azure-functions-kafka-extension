import {
  app,
  HttpRequest,
  HttpResponseInit,
  InvocationContext,
  output,
} from "@azure/functions";

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

export async function kafkaOutputWithHttp(
  request: HttpRequest,
  context: InvocationContext
): Promise<HttpResponseInit> {
  context.log(`Http function processed request for url "${request.url}"`);

  const body = await request.text();
  const queryName = request.query.get("name");
  const parsedbody = JSON.parse(body);
  const name = queryName || parsedbody.name || "world";
  context.extraOutputs.set(kafkaOutput, `Hello, ${parsedbody.name}!`);
  context.log(
    `Sending message to kafka: ${context.extraOutputs.get(kafkaOutput)}`
  );
  return {
    body: `Message sent to kafka with value: ${context.extraOutputs.get(
      kafkaOutput
    )}`,
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
