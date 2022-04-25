// This sample will create topic "topic" and send message to it. 
// KafkaTrigger will be trigged.
module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    const message = (req.query.message || (req.body && req.body.message));
    const responseMessage = message
        ? "Message received: " + message + ". The message transfered to the kafka broker."
        : "This HTTP triggered function executed successfully. Pass a message in the query string or in the request body for a personalized response.";
    context.bindings.outputKafkaMessage = "{ \"Offset\":364,\"Partition\":0,\"Topic\":\"kafkaeventhubtest1\",\"Timestamp\":\"2022-04-09T03:20:06.591Z\", \"Value\": \"" + message + "\", \"Headers\": [{ \"Key\": \"test\", \"Value\": \"javascript\" }] }"
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };
}