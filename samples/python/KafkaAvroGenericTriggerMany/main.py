import logging
import typing
from azure.functions import KafkaEvent

def main(kafkaAvroGenericTriggerMany : typing.List[KafkaEvent]):
    for event in kafkaAvroGenericTriggerMany:
        logging.info(event.get_body())