#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd "$DIR" || exit 1

export COMPOSE_INTERACTIVE_NO_CLI=1

# start docker compose
docker-compose up -d

# wait until kafka is ready to create topic
# need to improve, adding a retry instead of a static sleep
sleep 30

docker ps
