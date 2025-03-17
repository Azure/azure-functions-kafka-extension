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
  const parsedbody = JSON.parse(body);
  // assuming body is of the format { "key": "key", "value": {JSON object} }
  context.extraOutputs.set(
    kafkaOutput,
    `{ "Offset":364,"Partition":0,"Topic":"test-topic","Timestamp":"2022-04-09T03:20:06.591Z", "Value": "${JSON.stringify(
      parsedbody.value
    ).replace(/"/g, '\\"')}", "Key":"${
      parsedbody.key
    }", "Headers": [{ "Key": "language", "Value": "typescript" }] }`
  );
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
