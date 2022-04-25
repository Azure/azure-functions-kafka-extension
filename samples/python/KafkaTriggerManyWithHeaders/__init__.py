import logging
import typing
from azure.functions import KafkaEvent
import json
import base64

def main(kevents : typing.List[KafkaEvent]):
    for event in kevents:
        event_dec = event.get_body().decode('utf-8')
        event_json = json.loads(event_dec)
        logging.info("Python Kafka trigger function called for message " + event_json["Value"])
        headers = event_json["Headers"]
        for header in headers:
            logging.info("Key: "+ header['Key'] + " Value: "+ str(base64.b64decode(header['Value']).decode('ascii')))
