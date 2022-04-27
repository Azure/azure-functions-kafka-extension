module.exports = async function (context, event) {
  function print(kevent) {
    var keventJson = JSON.parse(kevent)
    context.log.info(`JavaScript Kafka trigger function called for message ${keventJson.Value}`);
    context.log.info(`Headers for this message:`)
    let headers =  keventJson.Headers;
    headers.forEach(element => {
        context.log.info(`Key: ${element.Key} Value:${Buffer.from(element.Value, 'base64')}`) 
    });
  }
  event.map(print);
};