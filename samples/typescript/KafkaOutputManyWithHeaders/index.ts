import { AzureFunction, Context, HttpRequest } from "@azure/functions"

const kafkaOutputMany: AzureFunction = async function (context: Context, req: HttpRequest): Promise<void> {
    const responseMessage = 'Ok'
    context.bindings.outputKafkaMessage =  ["{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"one\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"typescript\" }] }",
    "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"two\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"typescript\" }] }"];
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };

};

export default kafkaOutputMany;