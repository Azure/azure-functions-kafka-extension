FROM mcr.microsoft.com/azure-functions/powershell:4-powershell7-core-tools

# Copy Local File
RUN mkdir /workspace
COPY ./temp /workspace/temp
COPY ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/powershell/EventHub /workspace
WORKDIR /workspace

ENV LD_LIBRARY_PATH=/workspace/bin/runtimes/linux-x64/native

RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source
RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre

ENTRYPOINT [ "/bin/bash", "./start.sh" ]
