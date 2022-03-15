FROM mcr.microsoft.com/azure-functions/java:4-java8-core-tools

ARG WORK_DIR=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/EventHub
ARG TARGET_DIR=${WORK_DIR}/target/azure-functions/KafkaEventHubE2E
# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR ${WORK_DIR}
# dotnet 


RUN dotnet nuget add source /workspace/temp --name Local && dotnet nuget list source
RUN mvn clean package
ENV LD_LIBRARY_PATH=${TARGET_DIR}/bin/runtimes/linux-x64/native
# WORKDIR ${TARGET_DIR}

# RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre
WORKDIR ${WORK_DIR}

## ENTRYPOINT ["/usr/bin/tail", "-f", "/var/log/dpkg.log"]
ENTRYPOINT [ "/usr/bin/mvn", "azure-functions:run" ]
