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

const kafkaTrigger: AzureFunction = async function (context: Context, event_str: string[]): Promise<void> {

    for(var event of event_str) {
        context.log(event);
    }
};

export default kafkaTrigger;