import logging
from azure_functions.kafka import KafkaEvent

def main(kevent : KafkaEvent):
    logging.info(kevent.get_body().decode('utf-8'))