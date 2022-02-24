#!/bin/bash

FUNCTION_DIR="./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/java8"

pwd

export PATH=$PATH:/Azure.Functions.Cli
echo $PATH

chmod +x /Azure.Functions.Cli/func

func --version

cd $FUNCTION_DIR

func --version

# mvn clean package
# mvn azure-functions:run -e
