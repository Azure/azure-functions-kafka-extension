#!/bin/bash

CURRENT_DIR=`pwd`
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd $DIR/..

WORKING_DIR=temp
if [ -d "$WORKING_DIR" ]; then rm -rf $WORKING_DIR; fi

dotnet pack -o temp --include-symbols src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj /p:Version=100.100.100-pre

cd $CURRENT_DIR

docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/java8/Dockerfile -t jv8test . 
docker build -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/python38/Dockerfile -t py38test . 

# Docker Compose directory
# cd test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server