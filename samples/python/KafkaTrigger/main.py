import logging
from azure.functions_extensions.kafka import KafkaEvent

def main(kevent : KafkaEvent):
    logging.info(kevent.get_body().decode('utf-8'))
    logging.info(kevent.metadata)