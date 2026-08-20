# Kafka extension compatibility harness

This harness validates a declared Azure Functions component combination before any
package is published. The Java slice builds exact source commits, assembles them into
one runtime image, and verifies an HTTP -> Kafka trigger -> Kafka output round trip
against a local Docker-based Kafka broker.

## Source-pinned assembly

`manifests/*.json` is the source of truth for:

- Functions Host repository and commit
- Java worker repository and commit
- Java additions repository and commit
- Java library repository, commit, and Maven version
- Kafka extension workspace commit
- Function App Java/Maven plugin/compiler versions
- CFS-only NuGet and Maven endpoints
- expected JDK, Maven, and .NET SDK versions
- digest-pinned build, runtime, and broker images

Change `manifests/local-java.json` to exercise another Java combination. Git
components must resolve to immutable 40-character commits for release qualification.
The runner also checks that the selected build images report the manifest-declared
JDK, Maven, and .NET SDK versions, so descriptive toolchain values cannot drift from
the actual containers.

The build order is:

```text
java-additions -> java-library -> java-worker -> Java Function App
                                      |
Host source --------------------------+-> assembled runtime image
Kafka extension workspace package ---+
```

Java additions and the Java library are built with JDK 8 because their inherited
SpotBugs tooling is not compatible with JDK 21. Their artifacts are installed into an
isolated Maven repository. The worker and Function App are then built with JDK 21.
The selected Java library JAR is used both by the Function App and in the worker's
`annotationLib` directory.

The Host is published from the selected commit without bundled worker packages. The
selected worker is copied to `workers/java`, and the runtime starts
`Microsoft.Azure.WebJobs.Script.WebHost.dll` directly. Core Tools is used only during
the image build to materialize the workspace Kafka NuGet package and is not used to
start the Function App.

## CFS invariants

- The runner obtains a short-lived Azure DevOps token from the authenticated Azure CLI
  identity.
- Temporary authenticated NuGet and Maven files are created under `temp/compat/secrets`
  and deleted in `finally`.
- NuGet.org, Maven Central, and Sonatype are never configured directly.
- Public NuGet and Maven dependencies are routed through the CFS
  `upstream-public` feed.
- The CFS endpoints and Azure DevOps token resource are selected by the manifest.
- Host-only first-party feeds retain the Host repository's package-source mappings.
- Credentials are mounted as BuildKit secrets, never passed as build arguments or
  copied into image layers.
- The local Kafka extension package is package-mapped without replacing the CFS source.

## Run the Java/Local Kafka slice

Prerequisites are Docker, PowerShell 7, a .NET SDK, Azure CLI, and an Azure CLI identity
that can access the required Azure Artifacts feeds.

```powershell
pwsh ./test/CompatibilityHarness/run.ps1
```

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
  java-worker-layout.txt
  function-app/
    host.json
    extensions.json
    functions.txt
```

`summary.json` identifies the failed phase without parsing log text. Diagnostics are
captured before Compose cleanup on both success and failure. `provenance.json` records
resolved commits, toolchains, feed identities, container digests, the assembled image
ID, and SHA-256 values for the Host DLL, worker JAR, Java library JAR, Kafka extension
DLL, and local NuGet package. Credential values and temporary authenticated settings
are never included.

In Azure Pipelines, publish the diagnostics root even when the test fails:

```yaml
- pwsh: ./test/CompatibilityHarness/run.ps1
  displayName: Java Local Kafka compatibility E2E

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
