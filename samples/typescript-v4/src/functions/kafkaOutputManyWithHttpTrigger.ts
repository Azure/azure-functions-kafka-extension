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

export async function kafkaOutputManyWithHttp(
  request: HttpRequest,
  context: InvocationContext
): Promise<HttpResponseInit> {
  context.log(`Http function processed request for url "${request.url}"`);

  const queryName = request.query.get("name");
  const body = await request.text();
  const parsedbody = body ? JSON.parse(body) : {};
  parsedbody.name = parsedbody.name || "world";
  const name = queryName || parsedbody.name;
  context.extraOutputs.set(kafkaOutput, `Message one. Hello, ${name}!`);
  context.extraOutputs.set(kafkaOutput, `Message two. Hello, ${name}!`);
  return {
    body: `Messages sent to kafka.`,
    status: 200,
  };
}

const extraOutputs = [];
extraOutputs.push(kafkaOutput);

app.http("kafkaOutputManyWithHttp", {
  methods: ["GET", "POST"],
  authLevel: "anonymous",
  extraOutputs,
  handler: kafkaOutputManyWithHttp,
});
