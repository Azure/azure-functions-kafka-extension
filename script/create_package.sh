#!/bin/bash

CURRENT_DIR=`pwd`
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd $DIR/..

WORKING_DIR=temp
if [ -d "$WORKING_DIR" ]; then rm -rf $WORKING_DIR; fi

dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre

EXTENSION_SOURCE="${1:-package}"
EXTENSION_BUNDLE_VERSION="${2:-4.3.2}"

cd $CURRENT_DIR

docker build --build-arg EXTENSION_SOURCE="$EXTENSION_SOURCE" --build-arg EXTENSION_BUNDLE_VERSION="$EXTENSION_BUNDLE_VERSION" -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/EventHub/Dockerfile -t azure-functions-kafka-java-eventhub .
docker build --build-arg EXTENSION_SOURCE="$EXTENSION_SOURCE" --build-arg EXTENSION_BUNDLE_VERSION="$EXTENSION_BUNDLE_VERSION" -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/EventHub/Dockerfile -t azure-functions-kafka-python-eventhub .

docker build --build-arg EXTENSION_SOURCE="$EXTENSION_SOURCE" --build-arg EXTENSION_BUNDLE_VERSION="$EXTENSION_BUNDLE_VERSION" -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/Confluent/Dockerfile -t azure-functions-kafka-java-confluent .
docker build --build-arg EXTENSION_SOURCE="$EXTENSION_SOURCE" --build-arg EXTENSION_BUNDLE_VERSION="$EXTENSION_BUNDLE_VERSION" -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent/Dockerfile -t azure-functions-kafka-python-confluent .