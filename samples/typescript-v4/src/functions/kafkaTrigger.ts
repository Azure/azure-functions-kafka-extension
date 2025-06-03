import { app, InvocationContext } from "@azure/functions";

// This is a sample interface that describes the actual data in your event.
interface EventData {
  registertime: number;
  userid: string;
  regionid: string;
  gender: string;
}

export async function kafkaTrigger(
  event: any,
  context: InvocationContext
): Promise<void> {
  context.log("Event Offset: " + event.Offset);
  context.log("Event Partition: " + event.Partition);
  context.log("Event Topic: " + event.Topic);
  context.log("Event Timestamp: " + event.Timestamp);
  context.log("Event Value (as string): " + event.Value);

  let event_obj: EventData = JSON.parse(event.Value);

  context.log("Event Value Object: ");
  context.log("   Value.registertime: ", event_obj.registertime.toString());
  context.log("   Value.userid: ", event_obj.userid);
  context.log("   Value.regionid: ", event_obj.regionid);
  context.log("   Value.gender: ", event_obj.gender);
}

// eventhub
app.generic("Kafkatrigger", {
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
  handler: kafkaTrigger,
});

// confluent
// app.generic("Kafkatrigger", {
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
//     dataType: "string"
//   },
//   handler: kafkaTrigger,
// });
