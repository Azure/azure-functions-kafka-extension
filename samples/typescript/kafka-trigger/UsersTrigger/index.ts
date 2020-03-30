import { AzureFunction, Context } from "@azure/functions"
import { StringDecoder } from "string_decoder"


const kafkaTrigger: AzureFunction = async function (context: Context, event: Buffer): Promise<void> {
    const dec = new StringDecoder('utf-8');
    let event_str = dec.write(event);

    context.log(event_str);   
};

export default kafkaTrigger;