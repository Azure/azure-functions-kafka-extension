# Kafka extension compatibility harness

This harness validates a declared Azure Functions component combination before any
package is published. The first vertical slice runs a Java function against a local,
Docker-based Kafka broker and loads the Kafka extension from a package built from
the current workspace.

## Design

`manifests/*.json` is the source of truth for component identity:

- Functions Host image and digest
- language worker identity
- language library version or commit
- Kafka extension repository and commit
- broker image and digest

Artifact providers turn those identities into local artifacts. The initial provider
set is deliberately small:

| Component | Initial provider | Planned provider |
| --- | --- | --- |
| Functions Host | Digest-pinned Functions Java image | Build a selected Host commit |
| Java worker | Bundled in the Host image | Build and inject a selected worker commit |
| Java library | Exact Maven version through CFS | Build a selected library commit into an isolated Maven repository |
| Kafka extension | Current workspace commit packed into a local NuGet feed | Checkout and pack a selected commit |

The bundled worker mode is acceptable only for development qualification. A manifest
with `qualificationMode` set to `release` must identify Host and worker independently;
the runner rejects bundled worker mode for that purpose.

Each run writes the resolved component identities and locally produced package version
to `temp/compat/results/provenance.json`. This provenance file is the contract that a
future CI matrix and remote artifact providers will preserve.

## CFS invariants

- `CFS_NUGET_CONFIG` must point to an existing authenticated NuGet configuration.
- `CFS_MAVEN_SETTINGS` must point to Maven settings authenticated for the
  `upstream-public` server.
- NuGet restore and Docker extension installation use that configuration.
- The local Kafka extension package is added as a package-mapped source without
  replacing the CFS upstream source.
- Maven uses `FunctionApps/java/settings.xml`, whose `mirrorOf` is `*` and whose only
  remote endpoint is the CFS `upstream-public` feed.
- NuGet configuration is mounted as a BuildKit secret. It is not copied into an image.
- Package manager caches and temporary NuGet configuration are removed in the builder.
- The runner rejects direct NuGet.org sources. Maven's `mirrorOf=*` setting routes
  every Maven repository and plugin repository through CFS. The authenticated Maven
  settings are mounted as a BuildKit secret.

## Run the Java/Local Kafka slice

Prerequisites are Docker, PowerShell 7, .NET 8, and an authenticated CFS NuGet
configuration.

```powershell
$env:CFS_NUGET_CONFIG = "<absolute path to authenticated NuGet.config>"
$env:CFS_MAVEN_SETTINGS = "<absolute path to authenticated Maven settings.xml>"
pwsh ./test/CompatibilityHarness/run.ps1
```

The runner:

1. validates the component manifest and CFS configuration;
2. packs the workspace Kafka extension with a commit-derived prerelease version;
3. builds a Java Function image using only CFS and the local NuGet feed;
4. starts a KRaft Kafka broker and creates input/result topics;
5. sends a correlation token through HTTP output -> Kafka trigger -> Kafka output;
6. consumes the result and verifies the same token;
7. captures provenance and logs, then removes the environment.

Use `-KeepEnvironment` to retain containers after a successful run for debugging.

## Next providers

The next implementation step is a Git artifact provider with a shared contract:

```text
resolve(ref) -> immutable commit
build(commit, CFS inputs) -> artifact directory
describe(artifact) -> provenance entry
```

Host, Java worker, and Java library builders will each implement this contract. The
assembler will copy the selected worker into a selected Host publish layout, while the
Function App will consume the selected Java library from an isolated Maven repository.
The Local Kafka scenario and verifier remain unchanged.

In Azure Pipelines, create the two temporary files after `NuGetAuthenticate@1` and
`MavenAuthenticate@0`, pass their paths through the two environment variables, and
delete them under an `always()` cleanup step. Do not pass access tokens as Docker
build arguments or copy authenticated settings into the Docker context.
