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
    const key = new Date(timestamp).getHours().toString();
    const messageWithHeaders = `{ "Offset":364,"Partition":0,"Topic":"topic","Timestamp":"${timestamp}", "Value": "${message}", "Key": "${key}", "Headers": [{ "Key": "language", "Value": "typescript" }] }`;
    return [`1: ${messageWithHeaders}`, `2: ${messageWithHeaders}`];
  },
});
