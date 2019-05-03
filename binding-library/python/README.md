# Installation
pip install azure-functions-kafka-binding

# Usage

```python
from azure_functions.kafka import KafkaEvent

def kafkaConsume(kevent: KafkaEvent):
    print(kevent.get_body().decode('utf-8'))
```
