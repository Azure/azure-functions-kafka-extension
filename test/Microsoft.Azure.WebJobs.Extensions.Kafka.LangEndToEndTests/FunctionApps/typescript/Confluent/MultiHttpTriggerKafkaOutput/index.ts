import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    context.log('HTTP trigger function processed a request.');
    const message = req.query.message;
    const message1 = req.query.message1;
    const message2 = req.query.message2;
    const responseMessage = message
        ? "Messages received: " + message + " " + message1 +" " + message2 + ". The messages are transfered to the kafka broker."
        : "This HTTP triggered function executed successfully. Pass a message in the query string for a personalized response.";
    if (message1 && message1 && message2)
        context.bindings.outputKafkaMessage = [message, message1, message2];
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };

};

export default httpTrigger;