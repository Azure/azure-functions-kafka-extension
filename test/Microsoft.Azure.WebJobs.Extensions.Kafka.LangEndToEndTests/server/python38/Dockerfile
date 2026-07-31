#mcr.microsoft.com/azure-functions/python:3.0-python3.8-core-tools
FROM mcr.microsoft.com/azure-functions/python:3.0-python3.8-core-tools@sha256:9ef1b50c1502763b26f0a59eaa7ff58e9c891b0a1d73f43c03dcc1b87bde0a91

# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/python38
# dotnet 

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/python38/bin/runtimes/linux-x64/native

RUN python -m venv .venv 
RUN --mount=type=secret,id=pip_index_url,required=true \
    PIP_INDEX_URL="$(cat /run/secrets/pip_index_url)" \
    python -m pip install --no-cache-dir --requirement requirements.txt
ARG NUGET_CONFIG_PATH=/tmp/cfs/NuGet.config
RUN --mount=type=secret,id=nuget_config,required=true \
    bash /workspace/script/create_test_nuget_config.sh /run/secrets/nuget_config "${NUGET_CONFIG_PATH}" /workspace/temp \
    && RestoreConfigFile="${NUGET_CONFIG_PATH}" func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre \
    && dotnet nuget locals all --clear \
    && find /workspace -type d -name obj -prune -exec rm -rf {} + \
    && rm -f "${NUGET_CONFIG_PATH}"

ENTRYPOINT [ "/bin/bash", "./start.sh" ]
