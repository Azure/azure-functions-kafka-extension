FROM mcr.microsoft.com/azure-functions/node:4-node14-core-tools

# Copy Local File
RUN mkdir /workspace
COPY ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/javascript/Confluent /workspace
WORKDIR /workspace

ENV LD_LIBRARY_PATH=/workspace/bin/runtimes/linux-x64/native

RUN npm install
RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source
RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre

ENTRYPOINT [ "/bin/bash", "./start.sh" ]