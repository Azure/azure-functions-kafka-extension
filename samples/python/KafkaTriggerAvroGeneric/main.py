import logging
from azure.functions import KafkaEvent

def main(kafkaTriggerAvroGeneric : KafkaEvent):
    logging.info(kafkaTriggerAvroGeneric.get_body().decode('utf-8'))
    logging.info(kafkaTriggerAvroGeneric.metadata)