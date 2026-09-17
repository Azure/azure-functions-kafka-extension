# Kafka extension compatibility harness

This harness validates a declared Azure Functions component combination before any
package is published. It builds exact source commits, assembles them with the workspace
Kafka extension into one runtime image, and verifies an HTTP -> Kafka trigger -> Kafka
output round trip against a local Docker-based Kafka broker.

The current Local Kafka matrix covers:

| Worker runtime | Source-built components |
| --- | --- |
| Java | Host, Java worker, Java additions, Java library, Kafka extension |
| .NET isolated | Host, .NET isolated worker packages, Kafka extension |
| Node.js | Host, Node.js worker, Node.js library, Node.js extensions base, Kafka extension |

## Source-pinned assembly

`manifests/*.json` is the source of truth for:

- Functions Host repository and commit
- worker repository, language, and commit
- language libraries and extensions required by the selected worker
- Kafka extension workspace commit
- Function App package and toolchain versions
- CFS-only NuGet, Maven, and npm endpoints
- expected JDK, Maven, .NET SDK, and Node.js versions
- digest-pinned build, runtime, and broker images

Select or change one manifest to exercise another component combination:

| Manifest | Runtime |
| --- | --- |
| `manifests/local-java.json` | Java |
| `manifests/local-dotnet-isolated.json` | .NET isolated |
| `manifests/local-node.json` | Node.js |

Git components must resolve to immutable 40-character commits for release
qualification. The runner also checks that build images report the manifest-declared
toolchain versions, so descriptive values cannot drift from the actual containers.

The runtime-specific build chains are:

```text
Java:
  java-additions -> java-library -> java-worker -> Function App

.NET isolated:
  dotnet-worker -> local Worker NuGet packages -> Function App publish
                                                -> generated Host extensions

Node.js:
  node-extensions-base -> node-library -> Function App
  node-worker --------------------------> worker runtime

Host source + runtime artifacts + Kafka extension workspace package
  -> assembled runtime image
```

Java additions and the Java library are built with JDK 8 because their inherited
SpotBugs tooling is not compatible with JDK 21. Their artifacts are installed into an
isolated Maven repository. The worker and Function App are then built with JDK 21.
The selected Java library JAR is used both by the Function App and in the worker's
`annotationLib` directory.

The Host is published from the selected commit without bundled worker packages. The
selected worker is copied to its Host worker directory, and the runtime starts
`Microsoft.Azure.WebJobs.Script.WebHost.dll` directly. Core Tools is used only during
the image build to materialize the workspace Kafka NuGet package and is not used to
start the Function App.

For .NET isolated, the worker, SDK, source generators, analyzers, HTTP extension, and
Kafka worker extension are packed from the selected worker commit. The app publishes
against those local packages. The Kafka worker package metadata points the generated
Host extensions project at the workspace Kafka extension package.

For Node.js, the selected Node worker and `@azure/functions` library are built from
source. The selected Node extensions repository supplies
`@azure/functions-extensions-base`, which is installed into the Function App with the
source-built library. `node/package-lock.json` pins the resulting package graph and must
be refreshed when either source package changes its packed version or contents.

## CFS invariants

- The runner obtains a short-lived Azure DevOps token from the authenticated Azure CLI
  identity.
- Temporary authenticated NuGet, Maven, and npm files are created under
  `temp/compat/secrets` and deleted in `finally`.
- NuGet.org, Maven Central, Sonatype, and the public npm registry are never configured
  directly.
- Public NuGet, Maven, and npm dependencies are routed through the CFS
  `upstream-public` feed.
- The CFS endpoints and Azure DevOps token resource are selected by the manifest.
- Host-only first-party feeds retain the Host repository's package-source mappings.
- Credentials are mounted as BuildKit secrets, never passed as build arguments or
  copied into image layers.
- Local Kafka and .NET worker packages are package-mapped without replacing the CFS
  source.

## Run a Local Kafka slice

Prerequisites are Docker, PowerShell 7, a .NET SDK, Azure CLI, and an Azure CLI identity
that can access the required Azure Artifacts feeds.

```powershell
pwsh ./test/CompatibilityHarness/run.ps1 `
  -ManifestPath ./test/CompatibilityHarness/manifests/local-java.json

pwsh ./test/CompatibilityHarness/run.ps1 `
  -ManifestPath ./test/CompatibilityHarness/manifests/local-dotnet-isolated.json

pwsh ./test/CompatibilityHarness/run.ps1 `
  -ManifestPath ./test/CompatibilityHarness/manifests/local-node.json
```

The default manifest is `local-java.json`.

The runner validates the manifest, packs the workspace extension with a release-compatible
commit-derived prerelease version, builds the source-pinned image, starts Kafka, sends a
correlation token through both functions, and verifies the result topic. Use
`-KeepEnvironment` to retain the containers after a successful run.

Use `-DiagnosticsDirectory <path>` to select a different diagnostics root and
`-KeepEnvironment` to retain the containers after the diagnostics snapshot is taken.

## Diagnostics

Every run uses one correlation ID for the runner and Kafka message and writes an
isolated bundle under `temp/compat/results/<run-id>/`:

```text
manifest.json
provenance.json
summary.json
timeline.jsonl
logs/
  docker-build.log
  extension-build.log
  function.log
  kafka.log
  orchestrator.log
  toolchains.log
  verifier.log
state/
  compose-ps.jsonl
  container-inspect.json
  function-image-inspect.json
  <runtime>-worker-layout.txt
  function-app/
    host.json
    functions.txt
```

`summary.json` identifies the failed phase without parsing log text. Diagnostics are
captured before Compose cleanup on both success and failure. `provenance.json` records
resolved commits, toolchains, feed identities, container digests, the assembled image
ID, and SHA-256 values for the runtime-specific Host, worker, library, extension, and
Function App artifacts. Credential values and temporary authenticated settings are
never included.

In Azure Pipelines, publish the diagnostics root even when the test fails:

```yaml
- pwsh: ./test/CompatibilityHarness/run.ps1 -ManifestPath ./test/CompatibilityHarness/manifests/local-node.json
  displayName: Node.js Local Kafka compatibility E2E

- task: PublishPipelineArtifact@1
  displayName: Publish compatibility diagnostics
  condition: always()
  inputs:
    targetPath: temp/compat/results
    artifact: kafka-compat-diagnostics
```

## Extending the matrix

Additional brokers and languages should preserve the provider contract:

```text
resolve(ref) -> immutable commit
build(commit, CFS inputs) -> artifact directory
describe(artifact) -> provenance entry
```

The Local Kafka scenario and correlation verifier can remain unchanged while Host,
worker, library, and extension manifests vary.
