module.exports = async function (context, event) {
    context.log.info(`JavaScript Kafka trigger function called for message ${event.Value}`);
    throw "Unhandled Error"
};