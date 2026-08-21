#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 3 ]]; then
    echo "Usage: $0 <repository-config> <destination-config> <local-package-source>" >&2
    exit 2
fi

repository_config=$1
destination_config=$2
local_package_source=$3

mkdir -p "$(dirname "$destination_config")"
cp -- "$repository_config" "$destination_config"

dotnet_args=(
    nuget add source
    "$local_package_source"
    --name local-tests
    --configfile "$destination_config"
)
dotnet "${dotnet_args[@]}"

sed -i '/<packageSource key="upstream-public">/i\
    <packageSource key="local-tests">\
      <package pattern="Microsoft.Azure.WebJobs.Extensions.Kafka" />\
    <package pattern="Microsoft.Azure.Functions.Worker*" />\
    </packageSource>' "$destination_config"
