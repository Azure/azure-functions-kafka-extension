import { AzureFunction, Context } from "@azure/functions"

const kafkaTrigger: AzureFunction = async function (context: Context, event_str: string[]): Promise<void> {

    for(var event of event_str) {
        context.log(event);
    }
};

export default kafkaTrigger;