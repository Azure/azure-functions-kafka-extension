# syntax=docker/dockerfile:1.7

ARG DOTNET_SDK_IMAGE
ARG DOTNET_RUNTIME_IMAGE

FROM ${DOTNET_SDK_IMAGE} AS worker-build
ARG DOTNET_WORKER_REPOSITORY
ARG DOTNET_WORKER_COMMIT
ARG KAFKA_EXTENSION_VERSION
WORKDIR /workspace

RUN git clone --filter=blob:none "${DOTNET_WORKER_REPOSITORY}" dotnet-worker \
    && cd dotnet-worker \
    && git fetch --depth 1 origin "${DOTNET_WORKER_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${DOTNET_WORKER_COMMIT}" \
    && sed -i \
      "s#<WebJobsExtension Include=\"Microsoft.Azure.WebJobs.Extensions.Kafka\" Version=\"[^\"]*\" />#<WebJobsExtension Include=\"Microsoft.Azure.WebJobs.Extensions.Kafka\" Version=\"${KAFKA_EXTENSION_VERSION}\" />#" \
      extensions/Worker.Extensions.Kafka/src/Worker.Extensions.Kafka.csproj

RUN --mount=type=secret,id=nuget_config,target=/run/secrets/NuGet.config,required=true \
    dotnet restore dotnet-worker/DotNetWorker.sln \
      --configfile /run/secrets/NuGet.config \
    && dotnet restore dotnet-worker/DotNetWorker.Extensions.sln \
      --configfile /run/secrets/NuGet.config \
    && dotnet pack dotnet-worker/src/DotNetWorker.Core/DotNetWorker.Core.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/src/DotNetWorker.Grpc/DotNetWorker.Grpc.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/src/DotNetWorker/DotNetWorker.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/extensions/Worker.Extensions.Abstractions/src/Worker.Extensions.Abstractions.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/extensions/Worker.Extensions.Http/src/Worker.Extensions.Http.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/extensions/Worker.Extensions.Kafka/src/Worker.Extensions.Kafka.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/sdk/Sdk.Analyzers/Sdk.Analyzers.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/sdk/Sdk.Generators/Sdk.Generators.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false \
    && dotnet pack dotnet-worker/sdk/Sdk/Sdk.csproj \
      --configuration Release --no-restore --output /artifacts/packages \
      /p:IsLocalBuild=false

FROM ${DOTNET_SDK_IMAGE} AS app-build
ARG FUNCTION_TARGET_FRAMEWORK
ARG DOTNET_WORKER_VERSION
ARG DOTNET_WORKER_SDK_VERSION
ARG DOTNET_HTTP_EXTENSION_VERSION
ARG DOTNET_KAFKA_EXTENSION_VERSION
ARG KAFKA_NUPKG
WORKDIR /workspace

COPY --from=worker-build /artifacts/packages /workspace/packages
COPY ${KAFKA_NUPKG} /workspace/packages/
COPY script/create_test_nuget_config.sh /workspace/create_test_nuget_config.sh
COPY test/CompatibilityHarness/dotnet-isolated/ /workspace/app/

RUN --mount=type=secret,id=nuget_config,target=/run/secrets/NuGet.config,required=true \
    set -eu; \
    sed -i 's/\r$//' /workspace/create_test_nuget_config.sh; \
    mkdir -p /root/.nuget/NuGet; \
    trap 'rm -f /root/.nuget/NuGet/NuGet.Config' EXIT; \
    bash /workspace/create_test_nuget_config.sh \
      /run/secrets/NuGet.config \
      /root/.nuget/NuGet/NuGet.Config \
      /workspace/packages; \
    dotnet restore /workspace/app/CompatibilityApp.csproj \
      "/p:FunctionTargetFramework=${FUNCTION_TARGET_FRAMEWORK}" \
      "/p:FunctionsWorkerVersion=${DOTNET_WORKER_VERSION}" \
      "/p:FunctionsWorkerSdkVersion=${DOTNET_WORKER_SDK_VERSION}" \
      "/p:FunctionsHttpExtensionVersion=${DOTNET_HTTP_EXTENSION_VERSION}" \
      "/p:FunctionsKafkaExtensionVersion=${DOTNET_KAFKA_EXTENSION_VERSION}"; \
    dotnet publish /workspace/app/CompatibilityApp.csproj \
      --configuration Release --no-restore --output /artifacts/app \
      "/p:FunctionTargetFramework=${FUNCTION_TARGET_FRAMEWORK}" \
      "/p:FunctionsWorkerVersion=${DOTNET_WORKER_VERSION}" \
      "/p:FunctionsWorkerSdkVersion=${DOTNET_WORKER_SDK_VERSION}" \
      "/p:FunctionsHttpExtensionVersion=${DOTNET_HTTP_EXTENSION_VERSION}" \
      "/p:FunctionsKafkaExtensionVersion=${DOTNET_KAFKA_EXTENSION_VERSION}"; \
    test -f /artifacts/app/worker.config.json; \
    test -f /artifacts/app/functions.metadata; \
    test -f /artifacts/app/.azurefunctions/Microsoft.Azure.WebJobs.Extensions.Kafka.dll

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
ARG DOTNET_WORKER_COMMIT
ARG KAFKA_EXTENSION_VERSION

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated \
    HOME=/home \
    ASPNETCORE_URLS=http://0.0.0.0:80 \
    ASPNETCORE_CONTENTROOT=/azure-functions-host \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    HOST_COMMIT=${HOST_COMMIT} \
    DOTNET_WORKER_COMMIT=${DOTNET_WORKER_COMMIT} \
    KAFKA_EXTENSION_VERSION=${KAFKA_EXTENSION_VERSION}

COPY --from=host-build /artifacts/host /azure-functions-host
COPY --from=app-build /artifacts/app /home/site/wwwroot
COPY --from=app-build /artifacts/app/worker.config.json /azure-functions-host/workers/dotnet-isolated/worker.config.json

WORKDIR /azure-functions-host
EXPOSE 80
ENTRYPOINT ["dotnet", "Microsoft.Azure.WebJobs.Script.WebHost.dll"]
