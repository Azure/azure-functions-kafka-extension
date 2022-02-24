#!/bin/bash

FUNCTION_DIR="./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/java8"

Current_Dir = pwd

export PATH=$PATH:$Current_Dir/Azure.Functions.Cli
echo $PATH

chmod +x func

func --version

cd $FUNCTION_DIR

func --version

# mvn clean package
# mvn azure-functions:run -e
