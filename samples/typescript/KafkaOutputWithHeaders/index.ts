import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const kafkaOutput: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    const message = req.query.message;
    const responseMessage = 'Ok'
    context.bindings.outputKafkaMessage = "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"" + message + "\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"typescript\" }] }";
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };

};

export default kafkaOutput;