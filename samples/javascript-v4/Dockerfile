FROM mcr.microsoft.com/azure-functions/node:3.0-node10-core-tools As core-tools

COPY . /home/site/wwwroot

RUN cd /home/site/wwwroot && \
    func extensions install --javascript

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/node:3.0-appservice
FROM mcr.microsoft.com/azure-functions/node:3.0

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    LD_LIBRARY_PATH=/home/site/wwwroot/bin/runtimes/linux-x64/native

COPY . /home/site/wwwroot

RUN cd /home/site/wwwroot && \
    npm install