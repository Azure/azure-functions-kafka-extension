import logging
import typing
from azure.functions import KafkaEvent
import json

def main(kevents : typing.List[KafkaEvent]):
    for event in kevents:
        event_dec = event.get_body().decode('utf-8')
        event_json = json.loads(event_dec)
        logging.info(event_json['Value'])
        logging.info(event_json['Headers'])