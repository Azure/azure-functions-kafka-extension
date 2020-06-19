import logging
import typing
from azure.functions_extensions.kafka import KafkaEvent

def main(kevents : typing.List[KafkaEvent]):
    for event in kevents:
        logging.info(event.get_body())