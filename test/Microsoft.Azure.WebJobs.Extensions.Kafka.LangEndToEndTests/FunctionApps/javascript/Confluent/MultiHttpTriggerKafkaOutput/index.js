module.exports = async function (context, req) {
    const messages = ["message", "message1", "message2"]
        .map((name) => req.query[name] || (req.body && req.body[name]))
        .filter((value) => value);

    context.bindings.out = messages;
    context.res = {
        body: "Messages transferred to the kafka broker."
    };
};