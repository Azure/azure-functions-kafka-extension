#!/bin/bash

CURRENT_DIR=`pwd`
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd $DIR/..

WORKING_DIR=temp
if [ -d "$WORKING_DIR" ]; then rm -rf $WORKING_DIR; fi

dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre

cd $CURRENT_DIR

docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/EventHub/Dockerfile -t azure-functions-kafka-java-eventhub .
docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/EventHub/Dockerfile -t azure-functions-kafka-python-eventhub .

docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/Confluent/Dockerfile -t azure-functions-kafka-java-confluent .
docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent/Dockerfile -t azure-functions-kafka-python-confluent .