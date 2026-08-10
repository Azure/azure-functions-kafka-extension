#!/bin/bash

set -euo pipefail

CURRENT_DIR=$PWD
REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." >/dev/null 2>&1 && pwd)"
PROJECT_PATH="$REPOSITORY_ROOT/src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj"
NUGET_CONFIG_PATH="$REPOSITORY_ROOT/NuGet.config"
WORKING_DIR="$REPOSITORY_ROOT/temp"

if [[ -z "${PIP_INDEX_URL:-}" ]]; then
    echo "PIP_INDEX_URL must be set to the authenticated CFS Python feed." >&2
    exit 1
fi
if [[ -z "${CFS_NUGET_CONFIG:-}" ]]; then
    echo "CFS_NUGET_CONFIG must point to an authenticated temporary NuGet config." >&2
    exit 1
fi
DOCKER_NUGET_CONFIG_PATH=$CFS_NUGET_CONFIG
if [[ ! -f "$DOCKER_NUGET_CONFIG_PATH" ]]; then
    echo "Docker NuGet config does not exist: $DOCKER_NUGET_CONFIG_PATH" >&2
    exit 1
fi

cd "$REPOSITORY_ROOT"
trap 'cd "$CURRENT_DIR"' EXIT

if [[ -d "$WORKING_DIR" ]]; then
    rm -rf -- "$WORKING_DIR"
fi

restore_args=(
    restore
    "$PROJECT_PATH"
    --configfile "$NUGET_CONFIG_PATH"
)
dotnet "${restore_args[@]}"

for package_version in 100.100.100-pre 4.0.0; do
    pack_args=(
        pack
        "$PROJECT_PATH"
        --output "$WORKING_DIR"
        --include-symbols
        --no-restore
        "/p:Version=$package_version"
    )
    dotnet "${pack_args[@]}"
done

export DOCKER_BUILDKIT=1

docker build --secret "id=nuget_config,src=$DOCKER_NUGET_CONFIG_PATH" -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/EventHub/Dockerfile -t azure-functions-kafka-java-eventhub .
docker build --secret "id=nuget_config,src=$DOCKER_NUGET_CONFIG_PATH" --secret id=pip_index_url,env=PIP_INDEX_URL -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/EventHub/Dockerfile -t azure-functions-kafka-python-eventhub .

docker build --secret "id=nuget_config,src=$DOCKER_NUGET_CONFIG_PATH" -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/Confluent/Dockerfile -t azure-functions-kafka-java-confluent .
docker build --secret "id=nuget_config,src=$DOCKER_NUGET_CONFIG_PATH" --secret id=pip_index_url,env=PIP_INDEX_URL -f ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent/Dockerfile -t azure-functions-kafka-python-confluent .