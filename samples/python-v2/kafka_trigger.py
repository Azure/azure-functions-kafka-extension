import logging
import typing
import azure.functions as func
import json
import base64

KafkaTrigger = func.Blueprint()


@KafkaTrigger.function_name(name="KafkaTrigger")
@KafkaTrigger.kafka_trigger(
    arg_name="kevent",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    consumer_group="$Default1")
def kafka_trigger(kevent : func.KafkaEvent):
    logging.info(kevent.get_body().decode('utf-8'))
    logging.info(kevent.metadata)

@KafkaTrigger.function_name(name="KafkaTriggerMany")
@KafkaTrigger.kafka_trigger(
    arg_name="kevents",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    cardinality="MANY",
    data_type="string",
    consumer_group="$Default2")
def kafka_trigger_many(kevents : typing.List[func.KafkaEvent]):
    for event in kevents:
        logging.info(event.get_body())

@KafkaTrigger.function_name(name="KafkaTriggerWithHeaders")
@KafkaTrigger.kafka_trigger(
    arg_name="kevent",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    data_type="string",
    consumer_group="$Default3")
def kafka_trigger_with_headers(kevent : func.KafkaEvent):
    logging.info("Python Kafka trigger function called for message " + kevent.metadata["Value"])
    headers = json.loads(kevent.metadata["Headers"])
    for header in headers:
        logging.info("Key: "+ header['Key'] + " Value: "+ str(base64.b64decode(header['Value']).decode('ascii')))


@KafkaTrigger.function_name(name="KafkaTriggerManyWithHeaders")
@KafkaTrigger.kafka_trigger(
    arg_name="kevents",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    cardinality="MANY",
    data_type="string",
    consumer_group="$Default4")
def kafka_trigger_many_with_headers(kevents : typing.List[func.KafkaEvent]):
    for event in kevents:
        event_dec = event.get_body().decode('utf-8')
        event_json = json.loads(event_dec)
        logging.info("Python Kafka trigger function called for message " + event_json["Value"])
        headers = event_json["Headers"]
        for header in headers:
            logging.info("Key: "+ header['Key'] + " Value: "+ str(base64.b64decode(header['Value']).decode('ascii')))
