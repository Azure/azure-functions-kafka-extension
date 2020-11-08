import logging
from azure.functions import KafkaEvent
import azure.functions as func

def main(kevent : KafkaEvent, out : func.Out[str] ):
    event = kevent.get_body().decode('utf-8')
    logging.info(event)
    logging.info(kevent.metadata)
    out.set(event)
