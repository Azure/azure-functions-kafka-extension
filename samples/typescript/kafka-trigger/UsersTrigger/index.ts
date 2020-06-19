import { AzureFunction, Context } from "@azure/functions"

// This is to describe the metadata of a Kafka event
class KafkaEvent {
    Offset : number;
    Partition : number;
    Topic : string;
    Timestamp : string;
    Value : string;
    
    constructor(metadata:any) {
        this.Offset = metadata.Offset;
        this.Partition = metadata.Partition;
        this.Topic = metadata.Topic;
        this.Timestamp = metadata.Timestamp;
        this.Value = metadata.Value;
    }

    public getValue<T>() : T {
        return JSON.parse(this.Value).payload;
    }
}

// This is a sample interface that describes the actual data in your event.
interface EventData {
    registertime : number;
    userid : string;
    regionid: string;
    gender: string;
}

const kafkaTrigger: AzureFunction = async function (context: Context, event_str: string): Promise<void> {

    let event_obj = new KafkaEvent(eval(event_str));

    context.log("Event Offset: " + event_obj.Offset);
    context.log("Event Partition: " + event_obj.Partition);
    context.log("Event Topic: " + event_obj.Topic);
    context.log("Event Timestamp: " + event_obj.Timestamp);
    context.log("Event Value (as string): " + event_obj.Value);

    let event_value : EventData = event_obj.getValue<EventData>();
    
    context.log("Event Value Object: ");
    context.log("   Value.registertime: ", event_value.registertime.toString());
    context.log("   Value.userid: ", event_value.userid);
    context.log("   Value.regionid: ", event_value.regionid);
    context.log("   Value.gender: ", event_value.gender);
};

export default kafkaTrigger;