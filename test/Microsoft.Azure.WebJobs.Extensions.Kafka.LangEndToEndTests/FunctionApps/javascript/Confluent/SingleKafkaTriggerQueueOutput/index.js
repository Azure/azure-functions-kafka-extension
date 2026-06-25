function getKafkaValue(event) {
    if (event === undefined || event === null) {
        return "";
    }

    if (Buffer.isBuffer(event)) {
        return event.toString("utf8");
    }

    if (typeof event === "string") {
        try {
            const parsed = JSON.parse(event);
            return parsed.Value || parsed.value || event;
        } catch (error) {
            return event;
        }
    }

    return event.Value || event.value || String(event);
}

module.exports = async function (context, event) {
    const value = getKafkaValue(event);
    context.log(`JavaScript Kafka trigger function called for message ${value}`);
    context.bindings.queueMsg = value;
};