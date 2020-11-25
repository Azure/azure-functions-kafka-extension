FROM mcr.microsoft.com/azure-functions/java:3.0-java8-core-tools

ARG WORK_DIR=/workspace/test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/java8
ARG TARGET_DIR=${WORK_DIR}/target/azure-functions/kafka-function-20190419163130420
# Copy Local File
RUN mkdir /workspace
COPY . /workspace
WORKDIR ${WORK_DIR}
# dotnet 

ENV LD_LIBRARY_PATH=${TARGET_DIR}/bin/runtimes/linux-x64/native

RUN mvn clean package
WORKDIR ${TARGET_DIR}
RUN dotnet nuget add source /workspace/temp --name Local && dotnet nuget list source
RUN func extensions install --package Microsoft.Azure.WebJobs.Extensions.Kafka --version 100.100.100-pre
WORKDIR ${WORK_DIR}

## ENTRYPOINT ["/usr/bin/tail", "-f", "/var/log/dpkg.log"]
ENTRYPOINT [ "/usr/bin/mvn", "azure-functions:run" ]
