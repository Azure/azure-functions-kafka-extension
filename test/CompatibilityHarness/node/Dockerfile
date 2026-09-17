# syntax=docker/dockerfile:1.7

ARG NODE_IMAGE
ARG NODE_RUNTIME_IMAGE
ARG CORE_TOOLS_IMAGE
ARG DOTNET_SDK_IMAGE

FROM ${DOTNET_SDK_IMAGE} AS sources
ARG NODE_WORKER_REPOSITORY
ARG NODE_WORKER_COMMIT
ARG NODE_LIBRARY_REPOSITORY
ARG NODE_LIBRARY_COMMIT
ARG NODE_EXTENSIONS_REPOSITORY
ARG NODE_EXTENSIONS_COMMIT
WORKDIR /workspace

RUN git clone --filter=blob:none "${NODE_EXTENSIONS_REPOSITORY}" node-extensions \
    && cd node-extensions \
    && git fetch --depth 1 origin "${NODE_EXTENSIONS_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${NODE_EXTENSIONS_COMMIT}"
RUN git clone --filter=blob:none "${NODE_LIBRARY_REPOSITORY}" node-library \
    && cd node-library \
    && git fetch --depth 1 origin "${NODE_LIBRARY_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${NODE_LIBRARY_COMMIT}"
RUN git clone --filter=blob:none "${NODE_WORKER_REPOSITORY}" node-worker \
    && cd node-worker \
    && git fetch --depth 1 origin "${NODE_WORKER_COMMIT}" \
    && git checkout --detach FETCH_HEAD \
    && test "$(git rev-parse HEAD)" = "${NODE_WORKER_COMMIT}"

FROM ${NODE_IMAGE} AS node-build
WORKDIR /workspace
COPY --from=sources /workspace /workspace

RUN --mount=type=secret,id=npmrc,target=/home/.npmrc,required=true \
    cd /workspace/node-extensions/azure-functions-nodejs-extensions-base \
    && npm ci --ignore-scripts \
    && npm run build \
    && mkdir -p /artifacts \
    && npm pack --pack-destination /artifacts \
    && mv /artifacts/azure-functions-extensions-base-*.tgz \
      /artifacts/azure-functions-extensions-base.tgz

RUN --mount=type=secret,id=npmrc,target=/home/.npmrc,required=true \
    cd /workspace/node-library \
    && npm ci --ignore-scripts \
    && npm install --no-save /artifacts/azure-functions-extensions-base.tgz \
    && npm run build \
    && npm pack --pack-destination /tmp \
    && mv /tmp/azure-functions-*.tgz /artifacts/azure-functions.tgz

RUN --mount=type=secret,id=npmrc,target=/home/.npmrc,required=true \
    cd /workspace/node-worker \
    && npm ci --ignore-scripts \
    && npm run build \
    && test -f dist/src/nodejsWorker.js \
    && node -e "require('minimist')" \
    && mkdir -p /artifacts/worker \
    && cp -R dist node_modules package.json worker.config.json /artifacts/worker/

COPY test/CompatibilityHarness/node/package.json /workspace/app/package.json
COPY test/CompatibilityHarness/node/package-lock.json /workspace/app/package-lock.json
COPY test/CompatibilityHarness/node/host.json /workspace/app/host.json
COPY test/CompatibilityHarness/node/src /workspace/app/src
RUN --mount=type=secret,id=npmrc,target=/home/.npmrc,required=true \
    cd /workspace/app \
    && npm ci --omit=dev --ignore-scripts \
    && mkdir -p /artifacts/app \
    && cp -R host.json package.json package-lock.json node_modules src /artifacts/app/

FROM ${CORE_TOOLS_IMAGE} AS extension-build
ARG KAFKA_EXTENSION_VERSION
ARG KAFKA_NUPKG
COPY ${KAFKA_NUPKG} /workspace/packages/
COPY script/create_test_nuget_config.sh /workspace/create_test_nuget_config.sh
RUN --mount=type=secret,id=nuget_config,target=/run/secrets/NuGet.config,required=true \
    set -eu; \
    trap 'rm -f /tmp/compat-NuGet.config' EXIT; \
    sed -i 's/\r$//' /workspace/create_test_nuget_config.sh; \
    bash /workspace/create_test_nuget_config.sh \
      /run/secrets/NuGet.config \
      /tmp/compat-NuGet.config \
      /workspace/packages; \
    mkdir -p /artifacts/extensions; \
    cd /artifacts/extensions; \
    printf '{ "version": "2.0" }\n' > host.json; \
    RestoreConfigFile=/tmp/compat-NuGet.config func extensions install \
      --package Microsoft.Azure.WebJobs.Extensions.Kafka \
      --version "${KAFKA_EXTENSION_VERSION}"; \
    test -f bin/Microsoft.Azure.WebJobs.Extensions.Kafka.dll

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

FROM ${NODE_RUNTIME_IMAGE} AS runtime
ARG HOST_COMMIT
ARG NODE_WORKER_COMMIT
ARG NODE_LIBRARY_COMMIT
ARG NODE_EXTENSIONS_COMMIT
ARG KAFKA_EXTENSION_VERSION

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=node \
    HOME=/home \
    ASPNETCORE_URLS=http://0.0.0.0:80 \
    ASPNETCORE_CONTENTROOT=/azure-functions-host \
    HOST_COMMIT=${HOST_COMMIT} \
    NODE_WORKER_COMMIT=${NODE_WORKER_COMMIT} \
    NODE_LIBRARY_COMMIT=${NODE_LIBRARY_COMMIT} \
    NODE_EXTENSIONS_COMMIT=${NODE_EXTENSIONS_COMMIT} \
    KAFKA_EXTENSION_VERSION=${KAFKA_EXTENSION_VERSION}

RUN rm -rf /azure-functions-host /home/site/wwwroot/*
COPY --from=host-build /artifacts/host /azure-functions-host
COPY --from=node-build /artifacts/worker /azure-functions-host/workers/node
COPY --from=node-build /artifacts/app /home/site/wwwroot
COPY --from=extension-build /artifacts/extensions/bin /home/site/wwwroot/bin

WORKDIR /azure-functions-host
EXPOSE 80
ENTRYPOINT ["dotnet", "Microsoft.Azure.WebJobs.Script.WebHost.dll"]
