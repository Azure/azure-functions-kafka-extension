# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/node:2.0-appservice
FROM mcr.microsoft.com/azure-functions/node:2.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

# Install librdkafka from Confluent
RUN apt-get update && apt-get install -y wget gnupg apt-transport-https \
    && wget -qO - https://packages.confluent.io/deb/5.2/archive.key | apt-key add - \
    && apt-get install -y software-properties-common \
    && add-apt-repository "deb [arch=amd64] https://packages.confluent.io/deb/5.2 stable main" \
    && add-apt-repository "deb http://security.debian.org/debian-security jessie/updates main" \
    && apt-get update && apt install -y librdkafka-dev \
    && apt-get autoremove -y \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

COPY . /home/site/wwwroot

RUN cd /home/site/wwwroot && \
    npm install