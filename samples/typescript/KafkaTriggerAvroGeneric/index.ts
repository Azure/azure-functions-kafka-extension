import { AzureFunction, Context } from "@azure/functions"

const kafkaTrigger: AzureFunction = async function (context: Context, event_str: string): Promise<void> {
    context.log(event_str);
};

export default kafkaTrigger;