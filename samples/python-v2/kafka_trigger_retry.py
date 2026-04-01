import logging
import azure.functions as func

KafkaTriggerRetry = func.Blueprint()


@KafkaTriggerRetry.function_name(name="KafkaTriggerRetryFixed")
@KafkaTriggerRetry.kafka_trigger(
    arg_name="kevent",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    consumer_group="$Default")
@KafkaTriggerRetry.retry(strategy="fixed_delay", max_retry_count="3", delay_interval="00:00:10")
def kafka_trigger_retry_fixed(kevent : func.KafkaEvent):
    logging.info(kevent.get_body().decode('utf-8'))
    logging.info(kevent.metadata)
    raise Exception("unhandled error")


@KafkaTriggerRetry.function_name(name="KafkaTriggerRetryExponential")
@KafkaTriggerRetry.kafka_trigger(
    arg_name="kevent",
    topic="KafkaTopic",
    broker_list="KafkaBrokerList",
    username="KafkaUsername",
    password="KafkaPassword",
    protocol="SaslSsl",
    authentication_mode="Plain",
    consumer_group="$Default")
@KafkaTriggerRetry.retry(strategy="exponential_backoff", max_retry_count="5", minimum_interval="00:00:10", maximum_interval="00:15:00")
def kafka_trigger_retry_exponential(kevent : func.KafkaEvent):
    logging.info(kevent.get_body().decode('utf-8'))
    logging.info(kevent.metadata)
    raise Exception("unhandled error")