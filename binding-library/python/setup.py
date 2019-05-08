import setuptools
from setuptools import setup

with open("README.md") as f:
    long_description = f.read()

setup(
    name='azure-functions-kafka-binding',
    version='1.0.1',
    packages=['azure_functions.kafka'],
    long_description=long_description,
    long_description_content_type='text/markdown',
    license='MIT License',
    author='Microsoft Corporation',
    url='https://github.com/Azure/azure-functions-kafka-extension',
    install_requires=['azure-functions-worker'],
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Programming Language :: Python :: 3.6',
        'Operating System :: OS Independent',
        'License :: OSI Approved :: MIT License',
    ]
)
