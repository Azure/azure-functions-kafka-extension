import logging
from azure.functions import KafkaEvent, Out
import typing
import json 

def main(kevents : typing.List[KafkaEvent], queueMsg: Out[typing.List[str]]):
    messages = []
    logging.info(kevents)
    for kevent in kevents:
        event = kevent.get_body().decode('utf-8')
        logging.info(kevent.metadata)
        messages.append(json.loads(event)['Value'])
    queueMsg.set(messages)
