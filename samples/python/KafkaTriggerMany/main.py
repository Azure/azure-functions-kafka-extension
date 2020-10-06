import logging
import typing
from azure.functions import KafkaEvent

def main(kevents : typing.List[KafkaEvent]):
    for event in kevents:
        logging.info(event.get_body())