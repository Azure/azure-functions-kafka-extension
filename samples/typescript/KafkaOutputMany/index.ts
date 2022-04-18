import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const kafkaOutputMany: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    const responseMessage = 'Ok'
    context.bindings.outputKafkaMessage = ['one', 'two'];
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };

};

export default kafkaOutputMany;