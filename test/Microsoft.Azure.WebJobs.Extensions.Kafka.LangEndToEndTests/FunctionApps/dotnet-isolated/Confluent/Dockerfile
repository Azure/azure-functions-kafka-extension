FROM mcr.microsoft.com/dotnet/sdk:6.0 AS installer-env

# Build requires 3.1 SDK
COPY --from=mcr.microsoft.com/dotnet/core/sdk:3.1 /usr/share/dotnet /usr/share/dotnet

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated6.0-core-tools
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet-isolated/Confluent
# dotnet 

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet-isolated/Confluent/bin/.azurefunctions/runtimes/linux-x64/native

# RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source
RUN func extensions install

ENTRYPOINT [ "/bin/bash", "./start.sh" ]