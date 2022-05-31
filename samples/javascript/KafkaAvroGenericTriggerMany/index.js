module.exports = async function (context, events) {
    function print(event) {
        context.log.info(`JavaScript Kafka trigger function called for message ${JSON.stringify(event)}`);
    }
    events.map(print);
};