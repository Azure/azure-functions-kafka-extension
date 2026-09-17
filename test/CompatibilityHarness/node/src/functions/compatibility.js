const { app, output, trigger } = require("@azure/functions");

const inputKafka = trigger.generic({
  type: "kafkaTrigger",
  name: "event",
  direction: "in",
  brokerList: "kafka:29092",
  topic: "compat-input",
  consumerGroup: "compat-node",
  dataType: "string",
});

const inputKafkaOutput = output.generic({
  type: "kafka",
  name: "output",
  direction: "out",
  brokerList: "kafka:29092",
  topic: "compat-input",
  dataType: "string",
});

const resultKafkaOutput = output.generic({
  type: "kafka",
  name: "output",
  direction: "out",
  brokerList: "kafka:29092",
  topic: "compat-result",
  dataType: "string",
});

app.http("ProduceCompatibilityMessage", {
  methods: ["POST"],
  authLevel: "anonymous",
  extraOutputs: [inputKafkaOutput],
  handler: async (request, context) => {
    const message = await request.text();
    if (!message) {
      throw new Error("A correlation token is required.");
    }
    context.extraOutputs.set(inputKafkaOutput, message);
    return { status: 202, body: message };
  },
});

app.generic("RoundTripCompatibilityMessage", {
  trigger: inputKafka,
  extraOutputs: [resultKafkaOutput],
  handler: (message, context) => {
    context.extraOutputs.set(resultKafkaOutput, message);
  },
});
