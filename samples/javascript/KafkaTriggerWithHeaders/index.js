var string_decode = require('string_decoder').StringDecoder;

module.exports = async function (context, event) {
    // const dec = new string_decode('utf-8');
    // let event_str = dec.write(event);
    let event_str = event;
    context.log.info(`JavaScript Kafka trigger function called for message ${JSON.stringify(event_str)}`);
    console.log("Headers for this message:",context.bindingData.headers);
};