import azure.functions as func

from kafka_trigger_avro import KafkaTriggerAvro
from kafka_output import KafkaOutput
from kafka_trigger import KafkaTrigger
from kafka_trigger_retry import KafkaTriggerRetry

app = func.FunctionApp(http_auth_level=func.AuthLevel.ANONYMOUS)
app.register_blueprint(KafkaTriggerAvro)
app.register_blueprint(KafkaOutput)
app.register_blueprint(KafkaTrigger)
app.register_blueprint(KafkaTriggerRetry)