# syntax=docker/dockerfile:1.7

ARG JAVA_BUILD_IMAGE
ARG JAVA_ADDITIONS_BUILD_IMAGE
ARG DOTNET_SDK_IMAGE
ARG DOTNET_RUNTIME_IMAGE

FROM ${DOTNET_SDK_IMAGE} AS dotnet-sdk

FROM ${JAVA_ADDITIONS_BUILD_IMAGE} AS additions-build
ARG JAVA_ADDITIONS_REPOSITORY
ARG JAVA_ADDITIONS_COMMIT
ARG JAVA_LIBRARY_REPOSITORY
ARG JAVA_LIBRARY_COMMIT
ENV MAVEN_REPOSITORY=/workspace/m2
WORKDIR /workspace

RUN git clone --filter=blob:none "${JAVA_ADDITIONS_REPOSITORY}" java-additions \
    && cd java-additions \
    && git fetch --depth 1 origin "${JAVA_ADDITIONS_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${JAVA_ADDITIONS_COMMIT}"
RUN --mount=type=secret,id=maven_settings,target=/run/secrets/maven-settings.xml,required=true \
    mvn --batch-mode --no-transfer-progress \
      --settings /run/secrets/maven-settings.xml \
      -Dmaven.repo.local="${MAVEN_REPOSITORY}" \
      -Dgpg.skip \
      -Dspotbugs.skip=true \
      -Dmaven.test.skip=true \
      -DskipTests install \
      -f /workspace/java-additions/pom.xml
RUN git clone --filter=blob:none "${JAVA_LIBRARY_REPOSITORY}" java-library \
    && cd java-library \
    && git fetch --depth 1 origin "${JAVA_LIBRARY_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${JAVA_LIBRARY_COMMIT}"
RUN --mount=type=secret,id=maven_settings,target=/run/secrets/maven-settings.xml,required=true \
    mvn --batch-mode --no-transfer-progress \
      --settings /run/secrets/maven-settings.xml \
      -Dmaven.repo.local="${MAVEN_REPOSITORY}" \
      -Dgpg.skip \
      -Dspotbugs.skip=true \
      -DskipTests install \
      -f /workspace/java-library/pom.xml \
    && mkdir -p /artifacts/library \
    && find /workspace/java-library/target -maxdepth 1 -type f \
      -name 'azure-functions-java-library-*.jar' \
      ! -name '*sources*' \
      ! -name '*javadoc*' \
      -exec cp '{}' /artifacts/library/azure-functions-java-library.jar ';'

FROM ${JAVA_BUILD_IMAGE} AS java-build

ARG JAVA_WORKER_REPOSITORY
ARG JAVA_WORKER_COMMIT
ARG JAVA_LIBRARY_VERSION
ARG FUNCTION_APP_NAME
ARG FUNCTION_JAVA_VERSION
ARG FUNCTION_COMPILER_RELEASE
ARG AZURE_FUNCTIONS_MAVEN_PLUGIN_VERSION
ARG MAVEN_COMPILER_PLUGIN_VERSION
ARG KAFKA_EXTENSION_VERSION
ARG KAFKA_NUPKG

ENV MAVEN_REPOSITORY=/workspace/m2
WORKDIR /workspace

COPY --from=dotnet-sdk /usr/share/dotnet /usr/share/dotnet
COPY --from=additions-build /workspace/m2 /workspace/m2
COPY --from=additions-build /artifacts/library /artifacts/library

RUN git clone --filter=blob:none "${JAVA_WORKER_REPOSITORY}" java-worker \
    && cd java-worker \
    && git fetch --depth 1 origin "${JAVA_WORKER_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${JAVA_WORKER_COMMIT}"
RUN --mount=type=secret,id=maven_settings,target=/run/secrets/maven-settings.xml,required=true \
    mvn --batch-mode --no-transfer-progress \
      --settings /run/secrets/maven-settings.xml \
      -Dmaven.repo.local="${MAVEN_REPOSITORY}" \
      -Dazure.functions.java.library.version="${JAVA_LIBRARY_VERSION}" \
      -DskipTests package \
      -f /workspace/java-worker/pom.xml \
    && mkdir -p /artifacts/worker/annotationLib \
    && cp /workspace/java-worker/worker.config.json /artifacts/worker/worker.config.json \
    && worker_jar="$(find /workspace/java-worker/target -maxdepth 1 -type f \
      -name 'azure-functions-java-worker-*.jar' \
      ! -name '*-tests.jar' \
      ! -name '*sources*' \
      ! -name '*javadoc*')" \
    && test "$(printf '%s\n' "${worker_jar}" | grep -c .)" -eq 1 \
    && cp "${worker_jar}" /artifacts/worker/azure-functions-java-worker.jar \
    && cp /artifacts/library/azure-functions-java-library.jar \
      "/artifacts/worker/annotationLib/azure-functions-java-library-${JAVA_LIBRARY_VERSION}.jar" \
    && cp /workspace/java-worker/annotationLib/warmup-httptrigger.jar \
      /artifacts/worker/annotationLib/ \
    && test -f /artifacts/worker/azure-functions-java-worker.jar

COPY test/CompatibilityHarness/java/pom.xml /workspace/function-app/pom.xml
COPY test/CompatibilityHarness/java/host.json /workspace/function-app/host.json
COPY test/CompatibilityHarness/java/src /workspace/function-app/src
RUN --mount=type=secret,id=maven_settings,target=/run/secrets/maven-settings.xml,required=true \
    mvn --batch-mode --no-transfer-progress \
      --settings /run/secrets/maven-settings.xml \
      -Dmaven.repo.local="${MAVEN_REPOSITORY}" \
      -Dazure.functions.java.library.version="${JAVA_LIBRARY_VERSION}" \
      -Dazure.functions.maven.plugin.version="${AZURE_FUNCTIONS_MAVEN_PLUGIN_VERSION}" \
      -Dmaven.compiler.plugin.version="${MAVEN_COMPILER_PLUGIN_VERSION}" \
      -Dmaven.compiler.release="${FUNCTION_COMPILER_RELEASE}" \
      -Dfunction.java.version="${FUNCTION_JAVA_VERSION}" \
      -DfunctionAppName="${FUNCTION_APP_NAME}" \
      -DskipTests package \
      -f /workspace/function-app/pom.xml \
    && cp -R "/workspace/function-app/target/azure-functions/${FUNCTION_APP_NAME}" /artifacts/app

COPY ${KAFKA_NUPKG} /workspace/packages/
COPY script/create_test_nuget_config.sh /workspace/create_test_nuget_config.sh
RUN --mount=type=secret,id=nuget_config,target=/run/secrets/NuGet.config,required=true \
    set -eu; \
    sed -i 's/\r$//' /workspace/create_test_nuget_config.sh; \
    trap 'rm -f /tmp/compat-NuGet.config' EXIT; \
    bash /workspace/create_test_nuget_config.sh \
      /run/secrets/NuGet.config \
      /tmp/compat-NuGet.config \
      /workspace/packages; \
    cd /artifacts/app; \
    RestoreConfigFile=/tmp/compat-NuGet.config func extensions install \
      --package Microsoft.Azure.WebJobs.Extensions.Kafka \
      --version "${KAFKA_EXTENSION_VERSION}"; \
    test -f bin/Microsoft.Azure.WebJobs.Extensions.Kafka.dll; \
    dotnet nuget locals all --clear

FROM ${DOTNET_SDK_IMAGE} AS host-build

ARG HOST_REPOSITORY
ARG HOST_COMMIT
WORKDIR /workspace

RUN git clone --filter=blob:none "${HOST_REPOSITORY}" host \
    && cd host \
    && git fetch --depth 1 origin "${HOST_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${HOST_COMMIT}" \
    && printf '<Project />\n' > /workspace/empty-workers.props
RUN --mount=type=secret,id=ado_token,target=/run/secrets/ado-token,required=true \
    set -eu; \
    token="$(cat /run/secrets/ado-token)"; \
    cd /workspace/host; \
    for source in upstream-public Microsoft.Azure.Functions.PowerShellWorker 'AzureFunctions@internalrelease' AzureFunctionsRelease AzureFunctionsTempStaging AzureFunctionsInfra; do \
      dotnet nuget update source "${source}" \
        --configfile NuGet.config \
        --username AzureDevOps \
        --password "${token}" \
        --store-password-in-clear-text; \
    done; \
    dotnet publish src/WebJobs.Script.WebHost/WebJobs.Script.WebHost.csproj \
      --configuration Release \
      --output /artifacts/host \
      --property:WorkersProps=/workspace/empty-workers.props \
      --property:PublishReadyToRun=false; \
    git checkout -- NuGet.config

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime

ARG HOST_COMMIT
ARG JAVA_ADDITIONS_COMMIT
ARG JAVA_LIBRARY_COMMIT
ARG JAVA_WORKER_COMMIT
ARG FUNCTION_JAVA_VERSION
ARG KAFKA_EXTENSION_VERSION

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=java \
    HOME=/home \
    JAVA_HOME=/usr/lib/jvm/msft-${FUNCTION_JAVA_VERSION}-x64 \
    PATH=/usr/lib/jvm/msft-${FUNCTION_JAVA_VERSION}-x64/bin:${PATH} \
    ASPNETCORE_URLS=http://0.0.0.0:80 \
    ASPNETCORE_CONTENTROOT=/azure-functions-host \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    HOST_COMMIT=${HOST_COMMIT} \
    JAVA_ADDITIONS_COMMIT=${JAVA_ADDITIONS_COMMIT} \
    JAVA_LIBRARY_COMMIT=${JAVA_LIBRARY_COMMIT} \
    JAVA_WORKER_COMMIT=${JAVA_WORKER_COMMIT} \
    KAFKA_EXTENSION_VERSION=${KAFKA_EXTENSION_VERSION}

COPY --from=java-build /usr/lib/jvm/msft-${FUNCTION_JAVA_VERSION}-x64 /usr/lib/jvm/msft-${FUNCTION_JAVA_VERSION}-x64
COPY --from=host-build /artifacts/host /azure-functions-host
COPY --from=java-build /artifacts/worker /azure-functions-host/workers/java
COPY --from=java-build /artifacts/app /home/site/wwwroot

WORKDIR /azure-functions-host
EXPOSE 80
ENTRYPOINT ["dotnet", "Microsoft.Azure.WebJobs.Script.WebHost.dll"]
