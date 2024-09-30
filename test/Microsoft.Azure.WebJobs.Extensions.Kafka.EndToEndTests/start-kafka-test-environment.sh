#!/bin/bash

export COMPOSE_INTERACTIVE_NO_CLI=1

sudo apt install -y docker-compose

# start docker compose
docker-compose -f ./kafka-singlenode-compose.yaml  up --build -d

# wait until kafka is ready to create topic
# need to improve, adding a retry instead of a static sleep
sleep 30