import abc
import typing

class AbstractKafkaEvent(abc.ABC):

    @abc.abstractmethod
    def get_body(self) -> bytes:
        pass
    
    @abc.abstractmethod
    def key(self) -> typing.Optional[str]:
        pass
    
    @abc.abstractmethod
    def offset(self) -> typing.Optional[int]:
        pass
    
    @abc.abstractmethod
    def partition(self) -> typing.Optional[int]:
        pass
    
    @abc.abstractmethod
    def topic(self) -> typing.Optional[str]:
        pass
    
    @abc.abstractmethod
    def timestamp(self) -> typing.Optional[str]:
        pass
