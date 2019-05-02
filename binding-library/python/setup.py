from setuptools import setup

setup(
    name='azure-functions-kafka-binding',
    version='1.0.0',
    packages=['azure_functions.kafka'],
    install_requires=['azure-functions-worker']
)
