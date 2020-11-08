FROM mcr.microsoft.com/azure-functions/python:3.0-python3.8-core-tools

# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR /workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/python38
# dotnet 

ENV LD_LIBRARY_PATH=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/python38/bin/runtimes/linux-x64/native

RUN python -m venv .venv 
RUN pip install -r requirements.txt
RUN dotnet nuget add source /workspace/temp --name local && dotnet nuget list source
RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre

ENTRYPOINT [ "/bin/bash", "./start.sh" ]
