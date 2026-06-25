# Language E2E Test Harness

This folder contains the language end-to-end test harness for the Kafka extension. It builds Function App containers for Python, Java, and JavaScript, starts them locally in Docker, and exercises Kafka trigger/output scenarios against local Kafka and Azurite by default.

## Modes

The harness supports two installation modes for the Kafka extension:

- Package mode (default): installs the Kafka extension from the local package that is produced by the build.
- Bundle mode: relies on the Azure Functions extension bundle instead of explicitly installing the Kafka extension package.

This is controlled by two values that are passed to the Docker image build:

- `EXTENSION_SOURCE`
  - `package` for the default package-based path
  - `bundle` for the extension-bundle path
- `EXTENSION_BUNDLE_VERSION`
  - the exact bundle version to request, for example `4.3.2` or `4.37.0`
- `EXTENSION_BUNDLE_VERSION_RANGE`
  - optional advanced override for the full host.json bundle range, for example `[4.37.0,4.37.1)`
- `FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI`
  - optional bundle CDN override, for example `https://cdn-staging.functions.azure.com/public` for builds that have only reached staging

## How to use package mode

Package mode is the default behavior and is suitable for the existing CI and local flows.

### PowerShell

```powershell
./script/create_package.ps1
```

### Bash

```bash
./script/create_package.sh
```

This builds the test containers with `EXTENSION_SOURCE=package`. Existing CI uses this default path.

## How to use bundle mode

Bundle mode is useful when you want to validate the host’s extension bundle resolution path for a specific extension bundle version.

For local bundle release sanity, prefer the one-command runner:

```powershell
./script/run_bundle_lang_e2e.ps1 -ExtensionBundleVersion 4.37.0
```

The runner builds only the local Confluent Java/Python/JavaScript Function App images, starts local Kafka and Azurite, sets the fixed local Kafka and Azurite environment values, points bundle resolution at the staging CDN by default, runs the Confluent Lang E2E tests, writes a TRX file under `artifacts/LangE2E`, and cleans up the local containers.

To test a bundle from the production CDN instead of staging, pass `-BundleSource Production`:

```powershell
./script/run_bundle_lang_e2e.ps1 -ExtensionBundleVersion 4.37.0 -BundleSource Production
```

To test a custom bundle CDN, pass `-ExtensionBundleSourceUri <uri>`.

### PowerShell

```powershell
./script/create_package.ps1 -ExtensionSource bundle -ExtensionBundleVersion 4.37.0
```

### Bash

```bash
./script/create_package.sh bundle 4.37.0
```

The scripts bake the selected mode and bundle version into the Docker images. The test harness will not override those image defaults unless you explicitly set environment variables before running `dotnet test`.

When `EXTENSION_BUNDLE_VERSION` is a semantic version like `4.37.0`, the test harness generates a host bundle range of `[4.37.0,5.0.0)`, matching the OGF setup guidance for regular v4 bundle releases. This keeps the user-facing input as the bundle version you want to test while still using the interval notation required by the Functions host.

To override the image defaults during test execution, set:

```bash
export EXTENSION_SOURCE=bundle
export EXTENSION_BUNDLE_VERSION=4.37.0
export FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI=https://cdn-staging.functions.azure.com/public
```

For normal bundle validation, prefer the one-command runner or rebuild the images with the bundle arguments instead of relying only on runtime overrides.

## What changes between modes

### Package mode

- The test app container runs `func extensions install` (Python) or uses the package reference path in the app metadata (Java).
- The Kafka extension is resolved from the locally built package.

### Bundle mode

- The test app container uses the Functions extension bundle configuration from `host.json`.
- The Kafka extension is resolved by the host from the bundle instead of an explicit package install.
- The generated `host.json` uses a build-to-next-major range like `[4.37.0,5.0.0)`.
- This is useful for validating that the bundle selection and download flow works for the version you specify.
- For bundle versions that are only on staging CDN, set `FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI=https://cdn-staging.functions.azure.com/public` before running `dotnet test`.

## Running the test project

The unit-level harness configuration tests can be run with:

```bash
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.csproj --filter "FullyQualifiedName~BundleModeConfigurationTests"
```

## Running the local end-to-end flow

The default end-to-end path does not require Azure resources or a Confluent Cloud account. It uses the historical Confluent test app folders against local Kafka.

1. Build the Function App test images in package or bundle mode.
2. Start local Kafka and Azurite.
3. Run the Confluent-named Python, Java, and JavaScript tests.
4. Stop the local infrastructure.

### PowerShell

```powershell
./script/create_package.ps1 -ExtensionSource bundle -ExtensionBundleVersion 4.37.0
$env:FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI = "https://cdn-staging.functions.azure.com/public"
Push-Location ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server
docker-compose up -d
docker exec broker cub kafka-ready -b broker:29092 1 60
Pop-Location
dotnet test ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.csproj --filter "FullyQualifiedName~ConfluentAppTest"
Push-Location ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server
docker-compose down -v
Pop-Location
```

### Bash

```bash
bash ./script/create_package.sh bundle 4.37.0
export FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI=https://cdn-staging.functions.azure.com/public
bash ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/start-kafka-test-environment.sh
dotnet test ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.csproj --filter "FullyQualifiedName~ConfluentAppTest"
bash ./test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server/stop-kafka-test-environment.sh
```

EventHub tests are skipped by default. Set `EnableEventHubsTestsFlag=true` only when you intentionally want to run the legacy EventHub path with Azure resources and connection-string based EventHub Kafka settings.

For more setup details, see [LocalSetup.md](LocalSetup.md).
