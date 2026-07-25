## Software Installation

Function apps for the test run in Docker containers. Install Docker before running the language E2E tests.

## Local Infrastructure

The default language E2E path uses local containers only:

1. Kafka broker: `broker:29092` inside the Docker network and `localhost:9092` from the host.
2. Azurite: queue storage emulator on host ports `10000`, `10001`, and `10002`.
3. Function App containers: started by the xUnit fixture with `docker run`.

For bundle release sanity, use the one-command local runner from the repo root:

```powershell
./script/run_bundle_lang_e2e.ps1 -ExtensionBundleVersion 4.37.0
```

The runner builds the local Confluent Java/Python/JavaScript Function App images, starts Kafka and Azurite, sets fixed local values for `ConfluentBrokerList`, `AzureWebJobsStorage`, and `AzureStorageQueueTestConnection`, runs the Confluent Lang E2E tests in bundle mode, writes a TRX result under `artifacts/LangE2E`, and cleans up the local containers. It uses `https://cdn-staging.functions.azure.com/public` as the bundle source by default.

To test a bundle that is already on the production CDN, switch the bundle source:

```powershell
./script/run_bundle_lang_e2e.ps1 -ExtensionBundleVersion 4.37.0 -BundleSource Production
```

To test a custom bundle CDN, pass `-ExtensionBundleSourceUri <uri>`.

Start the local infrastructure before running the tests.

PowerShell:

```powershell
Push-Location test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server
docker-compose up -d
docker exec broker cub kafka-ready -b broker:29092 1 60
Pop-Location
```

Bash:

```bash
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/start-kafka-test-environment.sh
```

Stop it after the run.

PowerShell:

```powershell
Push-Location test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server
docker-compose down -v
Pop-Location
```

Bash:

```bash
bash test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/stop-kafka-test-environment.sh
```

## Default Environment Variables

No Azure or Confluent Cloud account variables are required for the default local path.

The test harness supplies these defaults when the variables are not set:

1. `ConfluentBrokerList`: defaults to `broker:29092` for the Function App containers. The historical Confluent test path now targets the local Kafka broker.
2. `AzureWebJobsStorage`: defaults to an Azurite connection string that uses `azurite` from Function App containers.
3. `AzureStorageQueueTestConnection`: optional test-process override. If unset, the xUnit verifier uses an Azurite connection string that targets `127.0.0.1`.

For bundle builds that are only available from the staging CDN, set this before running `dotnet test`:

PowerShell:

```powershell
$env:FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI = "https://cdn-staging.functions.azure.com/public"
```

Bash:

```bash
export FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI=https://cdn-staging.functions.azure.com/public
```

When `EXTENSION_BUNDLE_VERSION=4.37.0`, the harness writes the bundle range as `[4.37.0,5.0.0)`.

## EventHub Tests

EventHub tests are disabled by default. They are not part of the local sanity path.

To opt in to the legacy EventHub path, set:

```bash
export EnableEventHubsTestsFlag=true
```

The legacy EventHub path still requires Azure resources and EventHub connection settings:

1. `EventHubBrokerList`: Event Hubs Kafka endpoint.
2. `EventHubConnectionString`: Event Hubs connection string with manage, read, and write permissions.
3. Azure credentials resolvable by `DefaultAzureCredential` for creating and deleting Event Hubs under the configured resource group and namespace. Locally, `az login` and `az account set --subscription <subscription-id>` can satisfy this.

## Legacy Confluent Cloud Path

The language E2E tests no longer require Confluent Cloud credentials by default. The old `ConfluentCloudUsername` and `ConfluentCloudPassword` settings are not used by the local path.