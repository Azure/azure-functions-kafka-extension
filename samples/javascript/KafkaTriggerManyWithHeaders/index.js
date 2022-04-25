const { StringDecoder } = require('string_decoder');

module.exports = async function (context, event) {
  function print(kevent) {
    const dec = new StringDecoder('utf-8');
    let value = dec.write(kevent[0]);
    // context.log.info(event_str)
    // var event_json = JSON.parse(event_str);
    context.log.info(`JavaScript Kafka trigger function called for message ${value}`);
    context.log.info(`Headers for this message:`)
    let headers =  kevent[1];
    headers.forEach(element => {
        context.log.info(`Key: ${element.Key} Value:${Buffer.from(element.Value, 'base64')}`) 
    });
  }
  var kevent = event.map(function(e, i) {
    return [e, context.bindingData.headersArray[i]];
  });
  kevent.map(print);
};