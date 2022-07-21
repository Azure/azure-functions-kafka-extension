#!/bin/bash

#FUNCTION_DIR="./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent"

func --version

cd $FUNCTION_DIR
func extensions install
func start --python

#mvn clean package
#mvn azure-functions:run -e