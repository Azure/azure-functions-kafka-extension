module.exports = async function (context, req) {
    const message = req.query.message || (req.body && req.body.message);

    if (message) {
        context.bindings.out = message;
        context.res = {
            body: `Message received: ${message}. The message transferred to the kafka broker.`
        };
    } else {
        context.res = {
            status: 200,
            body: "This HTTP triggered function executed successfully. Pass a message in the query string or in the request body."
        };
    }
};