// This sample will create topic "topic" and send message to it. 
// KafkaTrigger will be trigged.
module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');

    const message = (req.query.message);
    const message1 = (req.query.message1);
    const message2 = (req.query.message2);
    
    const responseMessage = message
        ? "Message received: " + message + message1 + message2 + ". The message transfered to the kafka broker."
        : "This HTTP triggered function executed successfully. Pass a message in the query string or in the request body for a personalized response.";
    context.bindings.outputKafkaMessages = [message, message1, message2];
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };
}