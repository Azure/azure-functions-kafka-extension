FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 AS dn8-sdk-image
FROM mcr.microsoft.com/azure-functions/java:4-java21-core-tools

RUN rm -f /etc/apt/sources.list.d/yarn.list /etc/apt/sources.list.d/yarn*.list \
	&& apt-get update \
	&& apt-get install -y --no-install-recommends azure-functions-core-tools-4 \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

ARG EXTENSION_SOURCE=package
ARG EXTENSION_BUNDLE_VERSION=4.3.2
ENV EXTENSION_SOURCE=${EXTENSION_SOURCE}
ENV EXTENSION_BUNDLE_VERSION=${EXTENSION_BUNDLE_VERSION}

COPY --from=dn8-sdk-image [ "/usr/share/dotnet", "/usr/share/dotnet" ]
ARG WORK_DIR=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/EventHub
ARG TARGET_DIR=${WORK_DIR}/target/azure-functions/KafkaEventHubE2E
ENV TARGET_DIR=${TARGET_DIR}
# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR ${WORK_DIR}
# dotnet 

RUN dotnet nuget add source /workspace/temp --name Local && dotnet nuget list source
RUN mvn clean package
ENV LD_LIBRARY_PATH=${TARGET_DIR}/bin/runtimes/linux-x64/native
# WORKDIR ${TARGET_DIR}

WORKDIR ${WORK_DIR}

ENTRYPOINT [ "/bin/bash", "./start.sh" ]
