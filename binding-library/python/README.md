# Azure Functions Python Binding for Kafka
This project provides Kafka binding for Azure Functions written in Python that makes it easy
to consume Kafka messages from one topic.

# Features
- Reading from a Kafka topic

# Getting Started

## Installation via PyPI
pip install azure-functions-kafka-binding

## Installation via Git
`git clone https://github.com/Azure/azure-functions-kafka-extension`

`cd ./bindings_library/python/`

`python setup.py install`

## Minimum Requirements
- Python 3.6
- See setup.py for dependencies

## Usage

```python
from azure_functions.kafka import KafkaEvent

def kafkaConsume(kevent: KafkaEvent):
    print(kevent.get_body().decode('utf-8'))
```
