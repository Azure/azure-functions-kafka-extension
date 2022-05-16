module.exports = async function (context, event) {
    context.log.info(`JavaScript Kafka trigger function called for message ${JSON.stringify(event)}`);
};