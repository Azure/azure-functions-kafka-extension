module.exports = async function (context, event) {
    context.log.info(`JavaScript Kafka trigger function called for message ${event.Value}`);
    context.log.info("Headers for this message:",context.bindingData.headers);
};