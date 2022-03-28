// This sample will create topic "topic" and send message to it. 
// KafkaTrigger will be trigged.
module.exports = async function (context, req) {
    context.log('JavaScript HTTP trigger function processed a request.');
    
    context.bindings.outputKafkaMessages = ["one", "two"];
    context.res = {
        // status: 200, /* Defaults to 200 */
        body: responseMessage
    };
}