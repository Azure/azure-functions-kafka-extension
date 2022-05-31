import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const kafkaOutput: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    const message = req.query.message;
    const responseMessage = 'Ok'
    context.bindings.outputKafkaMessage = message;
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };

};

export default kafkaOutput;