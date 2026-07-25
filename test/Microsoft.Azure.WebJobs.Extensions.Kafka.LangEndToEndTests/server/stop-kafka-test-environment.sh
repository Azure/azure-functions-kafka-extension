#!/bin/bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd "$DIR" || exit 1

export COMPOSE_INTERACTIVE_NO_CLI=1

docker-compose down -v
