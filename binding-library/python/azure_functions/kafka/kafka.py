import json
import typing

from ._kafka import AbstractKafkaEvent

from azure.functions_worker.bindings import meta
from azure.functions_worker import protos

class KafkaEvent(AbstractKafkaEvent):
    """A concrete implementation of Kafka event message type."""

    def __init__(self, *,
                 body: bytes,
                 key: typing.Optional[str]=None,
                 offset: typing.Optional[int]=None,
                 partition: typing.Optional[int]=None,
                 topic: typing.Optional[str]=None,
                 timestamp: typing.Optional[str]=None) -> None:
        self.__body = body
        self.__key = key
        self.__offset = offset
        self.__partition = partition
        self.__topic = topic
        self.__timestamp = timestamp
    
    def get_body(self) -> bytes:
        return self.__body
    
    @property
    def key(self) -> typing.Optional[str]:
        return self.__key
    
    @property
    def offset(self) -> typing.Optional[int]:
        return self.__offset
    
    @property
    def partition(self) -> typing.Optional[int]:
        return self.__partition
    
    @property
    def topic(self) -> typing.Optional[str]:
        return self.__topic
    
    @property
    def timestamp(self) -> typing.Optional[str]:
        return self.__timestamp
    
    def __repr__(self) -> str:
        return (
            f'<azure.KafkaEvent '
            f'key={self.key} '
            f'partition={self.offset} '
            f'offset={self.offset} '
            f'topic={self.topic} '
            f'timestamp={self.timestamp} '
            f'at 0x{id(self):0x}>'
        )

class KafkaConverter(meta.InConverter, meta.OutConverter,
                        binding='kafka'):

    @classmethod
    def check_input_type_annotation(
           cls, pytype: type, datatype: protos.BindingInfo.DataType) -> bool:
        if datatype is protos.BindingInfo.undefined:
            return issubclass(pytype, KafkaEvent)
        else:
            return False

    @classmethod
    def check_output_type_annotation(cls, pytype) -> bool:
        return (
            issubclass(pytype, (str, bytes))
            or (issubclass(pytype, typing.List)
                and issubclass(pytype.__args__[0], str))
        )

    @classmethod
    def from_proto(cls, data: protos.TypedData, *,
                   pytype: typing.Optional[type],
                   trigger_metadata) -> KafkaEvent:
        data_type = data.WhichOneof('data')

        if data_type == 'string':
            body = data.string.encode('utf-8')

        elif data_type == 'bytes':
            body = data.bytes

        elif data_type == 'json':
            body = data.json.encode('utf-8')

        else:
            raise NotImplementedError(
                f'unsupported event data payload type: {data_type}')

        return KafkaEvent(body=body)

    @classmethod
    def to_proto(cls, obj: typing.Any, *,
                 pytype: typing.Optional[type]) -> protos.TypedData:
        raise NotImplementedError(
                f'Output bindings are not supported for Kafka')


class KafkaTriggerConverter(KafkaConverter,
                               binding='kafkaTrigger', trigger=True):

    @classmethod
    def from_proto(cls, data: protos.TypedData, *,
                   pytype: typing.Optional[type],
                   trigger_metadata) -> KafkaEvent:
        data_type = data.WhichOneof('data')

        if data_type == 'string':
            body = data.string.encode('utf-8')

        elif data_type == 'bytes':
            body = data.bytes

        elif data_type == 'json':
            body = data.json.encode('utf-8')

        else:
            raise NotImplementedError(
                f'unsupported event data payload type: {data_type}')

        return KafkaEvent(
            body=body,
            timestamp=cls._decode_trigger_metadata_field(
                trigger_metadata, 'Timestamp', python_type=str),
            key=cls._decode_trigger_metadata_field(
                trigger_metadata, 'Key', python_type=str),
            partition=cls._decode_trigger_metadata_field(
                trigger_metadata, 'Partition', python_type=int),
            offset=cls._decode_trigger_metadata_field(
                trigger_metadata, 'Offset', python_type=int),
            topic=cls._decode_trigger_metadata_field(
                trigger_metadata, 'Topic', python_type=str)
        )
