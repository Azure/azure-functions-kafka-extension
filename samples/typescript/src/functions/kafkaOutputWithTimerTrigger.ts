import { app, InvocationContext, Timer, output } from "@azure/functions";

const kafkaOutput = output.generic({
    type: "kafka",
    direction: "out",
    brokerlist: "localhost:9092",
    topic: "topic_6",
});

app.timer("kafkaoutputwithtimer", {
    schedule: "*/15 * * * * *",
    return: kafkaOutput,
    handler: (kafkaoutputwithtimer, context) => {
        const message = new Date().toISOString();
        return '{ "Offset":364,"Partition":0,"Topic":"topic_6","Timestamp":"2022-04-09T03:20:06.591Z", "Value": "testMessageValue", "Key": "testMessagekey", "Headers": [{ "Key": "test", "Value": "typescript" }] }';
    },
});
