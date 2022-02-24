#!/bin/bash

FUNCTION_DIR="./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/java8"

cd $FUNCTION_DIR

export PATH=$PATH:/Azure.Functions.Cli

mvn clean package
mvn azure-functions:run
