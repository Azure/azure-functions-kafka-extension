FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated6.0-core-tools
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet-isolated/Confluent
# dotnet 

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet-isolated/Confluent/bin/output/.azurefunctions/runtimes/linux-x64/native

# RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source
# RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre

ENTRYPOINT [ "/bin/bash", "./start.sh" ]