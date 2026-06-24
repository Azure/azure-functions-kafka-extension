FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim-amd64 AS dn8-sdk-image
FROM mcr.microsoft.com/azure-functions/python:4-python3.10-core-tools

ARG EXTENSION_SOURCE=package
ARG EXTENSION_BUNDLE_VERSION=4.3.2

COPY --from=dn8-sdk-image [ "/usr/share/dotnet", "/usr/share/dotnet" ]
# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/EventHub

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/EventHub/bin/runtimes/linux-x64/native
ENV EXTENSION_SOURCE=${EXTENSION_SOURCE}
ENV EXTENSION_BUNDLE_VERSION=${EXTENSION_BUNDLE_VERSION}

RUN python -m venv .venv
RUN pip install -r requirements.txt
RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source

RUN if [ "$EXTENSION_SOURCE" = "bundle" ]; then \
      echo "Using extension bundle mode"; \
    else \
      func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre; \
    fi

ENTRYPOINT [ "/bin/bash", "./start.sh" ]
