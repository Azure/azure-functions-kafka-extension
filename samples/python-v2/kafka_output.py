import azure.functions as func
import json

KafkaOutput = func.Blueprint()

@KafkaOutput.function_name(name="KafkaOutput")
@KafkaOutput.route(route="kafka_output")
@KafkaOutput.kafka_output(arg_name="outputMessage", topic="KafkaTopic", broker_list="KafkaBrokerList", username="KafkaUsername", password="KafkaPassword", protocol="SaslSsl", authentication_mode="Plain")
def kafka_output(req: func.HttpRequest, outputMessage: func.Out[str]) -> func.HttpResponse:
    input_msg = req.params.get('message')
    outputMessage.set(input_msg)
    return 'OK'


@KafkaOutput.function_name(name="KafkaOutputMany")
@KafkaOutput.route(route="kafka_output_many")
@KafkaOutput.kafka_output(arg_name="outputMessage", topic="KafkaTopic", broker_list="KafkaBrokerList", username="KafkaUsername", password="KafkaPassword", protocol="SaslSsl", authentication_mode="Plain", data_type="string")
def kafka_output_many(req: func.HttpRequest, outputMessage: func.Out[str] ) -> func.HttpResponse:
    outputMessage.set(json.dumps(['one', 'two']))
    return 'OK'

@KafkaOutput.function_name(name="KafkaOutputWithHeaders")
@KafkaOutput.route(route="kafka_output_with_headers")
@KafkaOutput.kafka_output(arg_name="out", topic="KafkaTopic", broker_list="KafkaBrokerList", username="KafkaUsername", password="KafkaPassword", protocol="SaslSsl", authentication_mode="Plain")
def kafka_output_with_headers(req: func.HttpRequest, out: func.Out[str]) -> func.HttpResponse:
    message = req.params.get('message')
    kevent =  { "Offset":0,"Partition":0,"Topic":"dummy","Timestamp":"2022-04-09T03:20:06.591Z", "Value": message, "Headers": [{ "Key": "test", "Value": "python" }] }
    out.set(json.dumps(kevent))
    return 'OK'

@KafkaOutput.function_name(name="KafkaOutputManyWithHeaders")
@KafkaOutput.route(route="kafka_output_many_with_headers")
@KafkaOutput.kafka_output(arg_name="out", topic="KafkaTopic", broker_list="KafkaBrokerList", username="KafkaUsername", password="KafkaPassword", protocol="SaslSsl", authentication_mode="Plain")
def kafka_output_many_with_headers(req: func.HttpRequest, out: func.Out[str]) -> func.HttpResponse:
    kevent = [{ "Offset": 364, "Partition":0,"Topic":"kafkaeventhubtest1","Timestamp":"2022-04-09T03:20:06.591Z", "Value": "one", "Headers": [{ "Key": "test", "Value": "python" }]  }, 
            { "Offset": 364, "Partition":0,"Topic":"kafkaeventhubtest1","Timestamp":"2022-04-09T03:20:06.591Z", "Value": "two", "Headers": [{ "Key": "test", "Value": "python" }]  }]
    out.set(json.dumps(kevent))
    return 'OK'