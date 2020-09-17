FROM mcr.microsoft.com/azure-functions/java:3.0-java8-core-tools AS installer-env

COPY . /src/java-function-app
RUN cd /src/java-function-app && \
    mkdir -p /home/site/wwwroot && \
    mvn clean package && \
    cd ./target/azure-functions/ && \
    cd $(ls -d */|head -n 1) && \
    cp ../../../extensions.csproj . && \
    cp -a . /home/site/wwwroot
 
# This Zulu OpenJDK Dockerfile and corresponding Docker image are
# to be used solely with Java applications or Java application components
# that are being developed for deployment on Microsoft Azure or Azure Stack,
# and are not intended to be used for any other purpose.

# This image is ssh enabled
FROM mcr.microsoft.com/azure-functions/java:3.0-java8-appservice

# If you want to deploy outside of Azure, use this image
#FROM mcr.microsoft.com/azure-functions/java:3.0-java8

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Added for Kafka so library
ENV LD_LIBRARY_PATH=/home/site/wwwroot/bin/runtimes/linux-x64/native

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
