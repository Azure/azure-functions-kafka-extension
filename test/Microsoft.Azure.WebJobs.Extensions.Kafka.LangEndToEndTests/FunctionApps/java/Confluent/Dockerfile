FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 AS dn8-sdk-image
FROM mcr.microsoft.com/azure-functions/java:4-java21-core-tools

COPY --from=dn8-sdk-image [ "/usr/share/dotnet", "/usr/share/dotnet" ]

ARG WORK_DIR=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/Confluent
ARG TARGET_DIR=${WORK_DIR}/target/azure-functions/KafkaConfluentE2E
# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR ${WORK_DIR}
# dotnet 

ARG NUGET_CONFIG_PATH=/tmp/cfs/NuGet.config
RUN --mount=type=secret,id=nuget_config,required=true \
    bash /workspace/script/create_test_nuget_config.sh /run/secrets/nuget_config "${NUGET_CONFIG_PATH}" /workspace/temp \
    && RestoreConfigFile="${NUGET_CONFIG_PATH}" mvn clean package \
    && dotnet nuget locals all --clear \
    && find /workspace -type d -name obj -prune -exec rm -rf {} + \
    && rm -f "${NUGET_CONFIG_PATH}"
ENV LD_LIBRARY_PATH=${TARGET_DIR}/bin/runtimes/linux-x64/native
# WORKDIR ${TARGET_DIR}

# RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre
WORKDIR ${WORK_DIR}

## ENTRYPOINT ["/usr/bin/tail", "-f", "/var/log/dpkg.log"]
ENTRYPOINT [ "/usr/bin/mvn", "azure-functions:run" ]
