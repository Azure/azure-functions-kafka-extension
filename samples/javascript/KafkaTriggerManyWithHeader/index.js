const { StringDecoder } = require('string_decoder');

module.exports = async function (context, event) {
  function print(event) {
    const dec = new StringDecoder('utf-8');
    let event_str = dec.write(event);
    context.log.info(`JavaScript Kafka trigger function called for message ${event_str}`);
  }
  event.map(print);
  console.log("Headers:")
  console.log(context.bindingData.headersArray);
};