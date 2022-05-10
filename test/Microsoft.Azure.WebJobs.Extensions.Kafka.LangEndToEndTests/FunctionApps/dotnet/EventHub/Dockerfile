FROM mcr.microsoft.com/azure-functions/dotnet:4-dotnet6-core-tools
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet/EventHub
# dotnet 

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/dotnet/EventHub/bin/runtimes/linux-x64/native

RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source
RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre

ENTRYPOINT [ "/bin/bash", "./start.sh" ]