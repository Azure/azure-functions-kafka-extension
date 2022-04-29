import logging
from azure.functions import KafkaEvent
import json
import base64


def main(kevent : KafkaEvent):
    logging.info("Python Kafka trigger function called for message " + kevent.metadata["Value"])
    headers = json.loads(kevent.metadata["Headers"])
    for header in headers:
        logging.info("Key: "+ header['Key'] + " Value: "+ str(base64.b64decode(header['Value']).decode('ascii')))
