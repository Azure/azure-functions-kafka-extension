#!/bin/bash

export COMPOSE_INTERACTIVE_NO_CLI=1

# start docker compose
docker-compose -f ./kafka-singlenode-compose.yaml  up --build -d

# wait until kafka is ready to create topic
# need to improve, adding a retry instead of a static sleep
sleep 30

docker-compose -f ./kafka-singlenode-compose.yaml exec kafka kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 1 --topic stringTopicOnePartition
docker-compose -f ./kafka-singlenode-compose.yaml exec kafka kafka-topics --create --zookeeper zookeeper:2181 --replication-factor 1 --partitions 10 --topic stringTopicTenPartitions