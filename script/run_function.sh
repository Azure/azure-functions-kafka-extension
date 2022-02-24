#!/bin/bash

FUNCTION_DIR="./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/java8"

export PATH=$PATH:./Azure.Functions.Cli
echo $PATH

# func --version

cd $FUNCTION_DIR

mvn clean package
mvn azure-functions:run
