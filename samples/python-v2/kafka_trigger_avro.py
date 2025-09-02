import logging
import typing
import azure.functions as func

KafkaTriggerAvro = func.Blueprint()

@KafkaTriggerAvro.function_name(name="KafkaTriggerAvroMany")
@KafkaTriggerAvro.kafka_trigger(
    arg_name="kafkaAvroGenericTriggerMany",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    data_type="string",
    consumer_group="$Default",
    avro_schema= "{\"type\":\"record\",\"name\":\"Payment\",\"namespace\":\"io.confluent.examples.clients.basicavro\",\"fields\":[{\"name\":\"id\",\"type\":\"string\"},{\"name\":\"amount\",\"type\":\"double\"},{\"name\":\"type\",\"type\":\"string\"}]}",
    cardinality="MANY")
def kafka_trigger_avro_many(kafkaAvroGenericTriggerMany : typing.List[func.KafkaEvent]):
    for event in kafkaAvroGenericTriggerMany:
        logging.info(event.get_body())

@KafkaTriggerAvro.function_name(name="KafkaTriggerAvroOne")
@KafkaTriggerAvro.kafka_trigger(
    arg_name="kafkaTriggerAvroGeneric",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    consumer_group="$Default",
    avro_schema= "{\"type\":\"record\",\"name\":\"Payment\",\"namespace\":\"io.confluent.examples.clients.basicavro\",\"fields\":[{\"name\":\"id\",\"type\":\"string\"},{\"name\":\"amount\",\"type\":\"double\"},{\"name\":\"type\",\"type\":\"string\"}]}")
def kafka_trigger_avro_one(kafkaTriggerAvroGeneric : func.KafkaEvent):
    logging.info(kafkaTriggerAvroGeneric.get_body().decode('utf-8'))
    logging.info(kafkaTriggerAvroGeneric.metadata)