# Language E2E Test Harness

This folder contains the language end-to-end test harness for the Kafka extension. It builds Function App containers for Python and Java, starts them locally in Docker, and exercises Kafka trigger/output scenarios against real broker endpoints.

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

### PowerShell

```powershell
./script/create_package.ps1 -ExtensionSource bundle -ExtensionBundleVersion 4.37.0
```

### Bash

```bash
./script/create_package.sh bundle 4.37.0
```

The scripts bake the selected mode and bundle version into the Docker images. The test harness will not override those image defaults unless you explicitly set environment variables before running `dotnet test`.

To override the image defaults during test execution, set:

```bash
export EXTENSION_SOURCE=bundle
export EXTENSION_BUNDLE_VERSION=4.37.0
```

For normal bundle validation, prefer rebuilding the images with the bundle arguments instead of relying only on runtime overrides.

## What changes between modes

### Package mode

- The test app container runs `func extensions install` (Python) or uses the package reference path in the app metadata (Java).
- The Kafka extension is resolved from the locally built package.

### Bundle mode

- The test app container uses the Functions extension bundle configuration from `host.json`.
- The Kafka extension is resolved by the host from the bundle instead of an explicit package install.
- The generated `host.json` uses an exact bundle version range like `[4.37.0]`.
- This is useful for validating that the bundle selection and download flow works for the version you specify.

## Running the test project

The unit-level harness configuration tests can be run with:

```bash
dotnet test test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.csproj --filter "FullyQualifiedName~BundleModeConfigurationTests"
```

For the full end-to-end flow, follow the setup steps in [LocalSetup.md](LocalSetup.md), then run the Lang E2E suite with your selected mode.
