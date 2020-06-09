#!/bin/bash
# TODO remove this shell program once Extension Bundle is supporeted. 

pushd . 
cd target/azure-functions/kafka-function-20190419163130420/
func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 2.0.0-beta
popd 