import { app, InvocationContext, Timer, output } from "@azure/functions";

const kafkaOutput = output.generic({
  type: "kafka",
  direction: "out",
  brokerlist: "%BrokerList%",
  topic: "topic",
});

app.timer("kafkaoutputwithtimer", {
  schedule: "*/15 * * * * *",
  return: kafkaOutput,
  handler: (kafkaoutputwithtimer, context) => {
    const timestamp = new Date().toISOString();
    const message = "Function triggered at " + timestamp;
    return [`1: ${message}`, `2: ${message}`];
  },
});
