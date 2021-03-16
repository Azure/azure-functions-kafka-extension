#!/bin/bash

export COMPOSE_INTERACTIVE_NO_CLI=1

# start docker compose
docker-compose -f ./kafka-singlenode-compose.yaml  down
