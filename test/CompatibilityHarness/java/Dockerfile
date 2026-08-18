# syntax=docker/dockerfile:1.7
ARG DOTNET_SDK_IMAGE
ARG FUNCTIONS_JAVA_IMAGE
FROM ${DOTNET_SDK_IMAGE} AS dotnet-sdk
FROM ${FUNCTIONS_JAVA_IMAGE} AS builder

ARG JAVA_LIBRARY_VERSION
ARG KAFKA_EXTENSION_VERSION
ARG WORK_DIR=/workspace/app
ARG TARGET_DIR=${WORK_DIR}/target/azure-functions/compat-java
ARG NUGET_CONFIG_PATH=/tmp/cfs/NuGet.config

COPY --from=dotnet-sdk /usr/share/dotnet /usr/share/dotnet
COPY test/CompatibilityHarness/java ${WORK_DIR}
WORKDIR ${WORK_DIR}

RUN --mount=type=secret,id=maven_settings,target=/root/.m2/settings.xml,required=true \
    mvn clean package \
    -Dazure.functions.java.library.version=${JAVA_LIBRARY_VERSION} \
    -Dmaven.javadoc.skip=true \
    -Dmaven.test.skip=true \
    -Dorg.slf4j.simpleLogger.log.org.apache.maven.cli.transfer.Slf4jMavenTransferListener=warn \
    -B

COPY temp/compat/nuget /workspace/nuget
COPY script/create_test_nuget_config.sh /workspace/create_test_nuget_config.sh
RUN --mount=type=secret,id=nuget_config,required=true \
    set -eu; \
    trap 'rm -f "${NUGET_CONFIG_PATH}"' EXIT; \
    bash /workspace/create_test_nuget_config.sh /run/secrets/nuget_config "${NUGET_CONFIG_PATH}" /workspace/nuget; \
    cd "${TARGET_DIR}"; \
    RestoreConfigFile="${NUGET_CONFIG_PATH}" func extensions install \
      --package Microsoft.Azure.WebJobs.Extensions.Kafka \
      --version "${KAFKA_EXTENSION_VERSION}"; \
    test -f bin/Microsoft.Azure.WebJobs.Extensions.Kafka.dll; \
    dotnet nuget locals all --clear; \
    rm -rf /root/.nuget/NuGet /root/.config/NuGet /workspace/nuget; \
    find /workspace -type d -name obj -prune -exec rm -rf {} +

ARG FUNCTIONS_JAVA_IMAGE
FROM ${FUNCTIONS_JAVA_IMAGE}
COPY --from=builder /workspace/app/target/azure-functions/compat-java /home/site/wwwroot
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    FUNCTIONS_WORKER_RUNTIME=java \
    LD_LIBRARY_PATH=/home/site/wwwroot/bin/runtimes/linux-x64/native
WORKDIR /home/site/wwwroot
ENTRYPOINT ["func", "start", "--port", "80", "--verbose"]
