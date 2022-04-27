module.exports = async function (context, events) {
    function print(event) {
        var eventJson = JSON.parse(event)
        context.log.info(`JavaScript Kafka trigger function called for message ${eventJson.Value}`);
    }
    events.map(print);
};