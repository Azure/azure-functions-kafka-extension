import logging
from azure.functions import KafkaEvent, Out

def main(kevent : KafkaEvent, queueMsg: Out[str]):
    logging.info(kevent.get_body().decode('utf-8'))
    logging.info(kevent.metadata)
    queueMsg.set(kevent.metadata['Value'])
