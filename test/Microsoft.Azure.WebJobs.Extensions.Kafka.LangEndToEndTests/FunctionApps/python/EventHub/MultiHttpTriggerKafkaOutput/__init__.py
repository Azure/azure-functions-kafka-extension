import logging
import typing
from azure.functions import KafkaEvent, Out, HttpRequest, HttpResponse
import json

def main(req: HttpRequest, out: Out[str] ) -> HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    message = req.params.get('message')
    message1 = req.params.get('message1')
    message2 = req.params.get('message2')
    if message and message1 and message2:
        messagesarr = [message, message1, message2]
        out.set(json.dumps(messagesarr))
        return HttpResponse(f"Message received: {message} {message1} {message2}. The message transfered to the kafka broker.")
    else:
        return HttpResponse(
             "This HTTP triggered function executed successfully. Pass a name in the query string for a personalized response.",
             status_code=200
        )
