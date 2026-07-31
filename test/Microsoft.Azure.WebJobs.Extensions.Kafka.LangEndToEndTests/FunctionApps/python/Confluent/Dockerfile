FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 AS dn8-sdk-image
FROM mcr.microsoft.com/azure-functions/python:4-python3.10-core-tools

COPY --from=dn8-sdk-image [ "/usr/share/dotnet", "/usr/share/dotnet" ]

# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent/bin/runtimes/linux-x64/native

RUN python -m venv .venv 
RUN --mount=type=secret,id=pip_index_url,required=true \
    set -eu; \
    trap 'rm -rf /root/.cache/pip' EXIT; \
    PIP_INDEX_URL="$(cat /run/secrets/pip_index_url)" python -m pip install --no-cache-dir --requirement requirements.txt
ARG NUGET_CONFIG_PATH=/tmp/cfs/NuGet.config
RUN --mount=type=secret,id=nuget_config,required=true \
    set -eu; \
    trap 'rm -f "${NUGET_CONFIG_PATH}"' EXIT; \
    bash /workspace/script/create_test_nuget_config.sh /run/secrets/nuget_config "${NUGET_CONFIG_PATH}" /workspace/temp; \
    RestoreConfigFile="${NUGET_CONFIG_PATH}" func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre; \
    test -f bin/Microsoft.Azure.WebJobs.Extensions.Kafka.dll; \
    dotnet nuget locals all --clear; \
    rm -rf /root/.nuget/NuGet /root/.config/NuGet; \
    find /workspace -type d -name obj -prune -exec rm -rf {} +

ENTRYPOINT [ "/bin/bash", "./start.sh" ]
