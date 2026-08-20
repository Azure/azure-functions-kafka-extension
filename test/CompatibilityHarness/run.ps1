[CmdletBinding()]
param(
    [string] $ManifestPath = (Join-Path $PSScriptRoot 'manifests/local-java.json'),
    [switch] $KeepEnvironment
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$projectName = 'kafka-compat'
$composePath = Join-Path $PSScriptRoot 'docker-compose.yml'
$workingDirectory = Join-Path $repositoryRoot 'temp\compat'
$nugetDirectory = Join-Path $workingDirectory 'nuget'
$resultsDirectory = Join-Path $workingDirectory 'results'
$secretsDirectory = Join-Path $workingDirectory 'secrets'
$functionImage = 'azure-functions-kafka-compat-java:local'
$environmentStarted = $false
$generatedSecretPaths = [System.Collections.Generic.List[string]]::new()
$assembledArtifactHashes = [ordered]@{}
$functionImageId = $null

function Invoke-Native {
    param(
        [Parameter(Mandatory)]
        [string] $Command,
        [Parameter(ValueFromRemainingArguments)]
        [string[]] $Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command failed with exit code $LASTEXITCODE."
    }
}

function Assert-CfsConfiguration {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'CFS_NUGET_CONFIG must point to an authenticated CFS NuGet configuration.'
    }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "CFS_NUGET_CONFIG does not exist: $Path"
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch 'pkgs\.dev\.azure\.com/.+/_packaging/') {
        throw 'CFS_NUGET_CONFIG does not contain an Azure Artifacts CFS source.'
    }
    if ($content -notmatch '<add\s+key="upstream-public"') {
        throw 'CFS_NUGET_CONFIG must name the CFS source upstream-public for package source mapping.'
    }
    if ($content -match 'api\.nuget\.org') {
        throw 'CFS_NUGET_CONFIG must not contain a direct NuGet.org source.'
    }
}

function Assert-CfsMavenSettings {
    param([string] $Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw 'CFS_MAVEN_SETTINGS must point to authenticated CFS Maven settings.'
    }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "CFS_MAVEN_SETTINGS does not exist: $Path"
    }

    [xml] $settings = Get-Content -LiteralPath $Path -Raw
    $mirror = $settings.SelectSingleNode(
        "/*[local-name()='settings']/*[local-name()='mirrors']/*[local-name()='mirror'][*[local-name()='id']='upstream-public']")
    $mirrorOf = $null
    $mirrorUrl = $null
    if ($null -ne $mirror) {
        $mirrorOf = $mirror.SelectSingleNode("*[local-name()='mirrorOf']").InnerText
        $mirrorUrl = $mirror.SelectSingleNode("*[local-name()='url']").InnerText
    }
    if ($null -eq $mirror -or $mirrorOf -ne '*' -or
        $mirrorUrl -notmatch 'pkgs\.dev\.azure\.com/.+/_packaging/upstream-public/maven/') {
        throw 'CFS_MAVEN_SETTINGS must mirror all repositories through upstream-public.'
    }

    $server = $settings.SelectSingleNode(
        "/*[local-name()='settings']/*[local-name()='servers']/*[local-name()='server'][*[local-name()='id']='upstream-public']")
    if ($null -eq $server) {
        throw 'CFS_MAVEN_SETTINGS must contain credentials for the upstream-public server.'
    }

    $raw = Get-Content -LiteralPath $Path -Raw
    if ($raw -match 'repo1\.maven\.org|repo\.maven\.apache\.org|oss\.sonatype\.org') {
        throw 'CFS_MAVEN_SETTINGS must not contain direct Maven Central or Sonatype endpoints.'
    }
}

function Assert-Manifest {
    foreach ($componentName in @('host', 'worker', 'javaAdditions', 'languageLibrary')) {
        $component = $manifest.$componentName
        if ($component.source -ne 'git' -or
            $component.repository -notmatch '^https://github\.com/Azure/' -or
            $component.commit -notmatch '^[a-f0-9]{40}$') {
            throw "$componentName must identify an Azure Git repository and an immutable 40-character commit."
        }
    }
    if ($manifest.host.runtimeImage -notmatch '@sha256:[a-f0-9]{64}$') {
        throw 'The Host runtime image must be digest-pinned.'
    }
    if ($manifest.kafkaExtension.source -ne 'workspace') {
        throw 'The initial Kafka extension provider supports only the current workspace.'
    }
    if ($manifest.broker.source -ne 'container' -or $manifest.broker.image -notmatch '@sha256:[a-f0-9]{64}$') {
        throw 'The Local Kafka provider requires a digest-pinned container image.'
    }
    if ($manifest.buildTooling.source -ne 'container' -or
        $manifest.buildTooling.javaAdditionsImage -notmatch '@sha256:[a-f0-9]{64}$' -or
        $manifest.buildTooling.javaImage -notmatch '@sha256:[a-f0-9]{64}$' -or
        $manifest.buildTooling.dotnetSdkImage -notmatch '@sha256:[a-f0-9]{64}$') {
        throw 'Java and .NET build tooling must use digest-pinned container images.'
    }
}

function New-CfsSecrets {
    New-Item -ItemType Directory -Path $secretsDirectory -Force | Out-Null
    $token = (& az account get-access-token `
        --resource '499b84ac-1321-427f-aa17-267ca6975798' `
        --query accessToken `
        --output tsv)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($token)) {
        throw 'Unable to acquire a short-lived Azure DevOps token from the current Azure CLI identity.'
    }
    $token = $token.Trim()

    $tokenPath = Join-Path $secretsDirectory 'ado-token'
    Set-Content -LiteralPath $tokenPath -Value $token -NoNewline
    $generatedSecretPaths.Add($tokenPath)

    $nugetPath = Join-Path $secretsDirectory 'NuGet.config'
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="upstream-public" value="https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/nuget/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <upstream-public>
      <add key="Username" value="AzureDevOps" />
      <add key="ClearTextPassword" value="$token" />
    </upstream-public>
  </packageSourceCredentials>
  <packageSourceMapping>
    <packageSource key="upstream-public">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -LiteralPath $nugetPath
    $generatedSecretPaths.Add($nugetPath)

    $mavenPath = Join-Path $secretsDirectory 'settings.xml'
    @"
<?xml version="1.0" encoding="UTF-8"?>
<settings xmlns="http://maven.apache.org/SETTINGS/1.0.0">
  <servers>
    <server>
      <id>upstream-public</id>
      <username>AzureDevOps</username>
      <password>$token</password>
    </server>
  </servers>
  <mirrors>
    <mirror>
      <id>upstream-public</id>
      <name>Azure Functions CFS</name>
      <url>https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/maven/v1</url>
      <mirrorOf>*</mirrorOf>
    </mirror>
  </mirrors>
</settings>
"@ | Set-Content -LiteralPath $mavenPath
    $generatedSecretPaths.Add($mavenPath)

    return @{
        TokenPath = $tokenPath
        NuGetPath = $nugetPath
        MavenPath = $mavenPath
    }
}

Assert-Manifest

$head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the Kafka extension workspace commit.'
}
if ($manifest.kafkaExtension.ref -ne 'HEAD' -and -not $head.StartsWith($manifest.kafkaExtension.ref)) {
    throw "Workspace commit $head does not match requested ref $($manifest.kafkaExtension.ref)."
}

$versionPropsPath = Join-Path $repositoryRoot 'build\common.props'
$versionMatch = [Regex]::Match(
    (Get-Content -LiteralPath $versionPropsPath -Raw),
    '<Version>(\d+\.\d+\.\d+)')
if (-not $versionMatch.Success) {
    throw "Unable to resolve the Kafka extension version from $versionPropsPath."
}
$packageVersion = "$($versionMatch.Groups[1].Value)-e2e.$($head.Substring(0, 12).ToLowerInvariant())"
$projectPath = Join-Path $repositoryRoot 'src\Microsoft.Azure.WebJobs.Extensions.Kafka\Microsoft.Azure.WebJobs.Extensions.Kafka.csproj'
$previousBuildKit = $env:DOCKER_BUILDKIT
$previousFunctionImage = $env:COMPAT_FUNCTION_IMAGE
$previousKafkaImage = $env:COMPAT_KAFKA_IMAGE

try {
    New-Item -ItemType Directory -Path $nugetDirectory, $resultsDirectory -Force | Out-Null
    $cfs = New-CfsSecrets
    Assert-CfsConfiguration -Path $cfs.NuGetPath
    Assert-CfsMavenSettings -Path $cfs.MavenPath

    Invoke-Native dotnet restore $projectPath --configfile $cfs.NuGetPath
    Invoke-Native dotnet pack $projectPath --output $nugetDirectory --include-symbols --no-restore "/p:Version=$packageVersion"
    $packagePath = Get-ChildItem -LiteralPath $nugetDirectory -Filter "Microsoft.Azure.WebJobs.Extensions.Kafka.$packageVersion.nupkg" |
        Select-Object -First 1
    if ($null -eq $packagePath) {
        throw "Kafka extension package $packageVersion was not produced."
    }
    $packageContextPath = [IO.Path]::GetRelativePath($repositoryRoot, $packagePath.FullName).Replace('\', '/')

    $env:DOCKER_BUILDKIT = '1'
    Invoke-Native docker build `
        --secret "id=ado_token,src=$($cfs.TokenPath)" `
        --secret "id=nuget_config,src=$($cfs.NuGetPath)" `
        --secret "id=maven_settings,src=$($cfs.MavenPath)" `
        --build-arg "JAVA_ADDITIONS_BUILD_IMAGE=$($manifest.buildTooling.javaAdditionsImage)" `
        --build-arg "JAVA_BUILD_IMAGE=$($manifest.buildTooling.javaImage)" `
        --build-arg "DOTNET_SDK_IMAGE=$($manifest.buildTooling.dotnetSdkImage)" `
        --build-arg "DOTNET_RUNTIME_IMAGE=$($manifest.host.runtimeImage)" `
        --build-arg "HOST_REPOSITORY=$($manifest.host.repository)" `
        --build-arg "HOST_COMMIT=$($manifest.host.commit)" `
        --build-arg "JAVA_ADDITIONS_REPOSITORY=$($manifest.javaAdditions.repository)" `
        --build-arg "JAVA_ADDITIONS_COMMIT=$($manifest.javaAdditions.commit)" `
        --build-arg "JAVA_LIBRARY_REPOSITORY=$($manifest.languageLibrary.repository)" `
        --build-arg "JAVA_LIBRARY_COMMIT=$($manifest.languageLibrary.commit)" `
        --build-arg "JAVA_LIBRARY_VERSION=$($manifest.languageLibrary.version)" `
        --build-arg "JAVA_WORKER_REPOSITORY=$($manifest.worker.repository)" `
        --build-arg "JAVA_WORKER_COMMIT=$($manifest.worker.commit)" `
        --build-arg "KAFKA_EXTENSION_VERSION=$packageVersion" `
        --build-arg "KAFKA_NUPKG=$packageContextPath" `
        --file (Join-Path $PSScriptRoot 'java\Dockerfile') `
        --tag $functionImage `
        $repositoryRoot
    $functionImageId = (& docker image inspect $functionImage --format '{{.Id}}').Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to resolve the assembled Function image ID.'
    }
    $artifactHashLines = & docker run --rm --entrypoint sha256sum $functionImage `
        /azure-functions-host/Microsoft.Azure.WebJobs.Script.WebHost.dll `
        /azure-functions-host/workers/java/azure-functions-java-worker.jar `
        "/azure-functions-host/workers/java/annotationLib/azure-functions-java-library-$($manifest.languageLibrary.version).jar" `
        /home/site/wwwroot/bin/Microsoft.Azure.WebJobs.Extensions.Kafka.dll
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to hash the assembled runtime artifacts.'
    }
    foreach ($line in $artifactHashLines) {
        if ($line -match '^([a-f0-9]{64})\s+(.+)$') {
            $assembledArtifactHashes[$Matches[2]] = $Matches[1]
        }
    }

    $env:COMPAT_FUNCTION_IMAGE = $functionImage
    $env:COMPAT_KAFKA_IMAGE = $manifest.broker.image
    Invoke-Native docker compose --project-name $projectName --file $composePath up --detach kafka
    $environmentStarted = $true

    Invoke-Native docker compose --project-name $projectName --file $composePath exec --no-TTY kafka `
        kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists `
        --topic compat-input --partitions 1 --replication-factor 1
    Invoke-Native docker compose --project-name $projectName --file $composePath exec --no-TTY kafka `
        kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists `
        --topic compat-result --partitions 1 --replication-factor 1

    Invoke-Native docker compose --project-name $projectName --file $composePath up --detach function

    $token = "compat-$([Guid]::NewGuid().ToString('N'))"
    $deadline = [DateTimeOffset]::UtcNow.AddMinutes(3)
    $invoked = $false
    while ([DateTimeOffset]::UtcNow -lt $deadline -and -not $invoked) {
        try {
            $response = Invoke-WebRequest `
                -Uri 'http://localhost:7071/api/ProduceCompatibilityMessage' `
                -Method Post `
                -Body $token `
                -ContentType 'text/plain' `
                -TimeoutSec 10
            $invoked = $response.StatusCode -eq 202
        }
        catch {
            Start-Sleep -Seconds 3
        }
    }
    if (-not $invoked) {
        throw 'The Java Function did not become ready within three minutes.'
    }

    $result = & docker compose --project-name $projectName --file $composePath exec --no-TTY kafka `
        kafka-console-consumer --bootstrap-server kafka:29092 --topic compat-result `
        --from-beginning --max-messages 1 --timeout-ms 60000 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Kafka result consumer failed: $($result -join [Environment]::NewLine)"
    }
    if (($result -join [Environment]::NewLine) -notmatch [Regex]::Escape($token)) {
        throw "The result topic did not contain correlation token $token."
    }

    $provenance = [ordered]@{
        manifest = $manifest.name
        qualificationMode = $manifest.qualificationMode
        kafkaExtension = [ordered]@{
            repository = $manifest.kafkaExtension.repository
            commit = $head
            packageVersion = $packageVersion
            packageSha256 = (Get-FileHash -LiteralPath $packagePath.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        host = $manifest.host
        worker = $manifest.worker
        javaAdditions = $manifest.javaAdditions
        languageLibrary = $manifest.languageLibrary
        broker = $manifest.broker
        buildTooling = $manifest.buildTooling
        assembledImageId = $functionImageId
        assembledArtifactSha256 = $assembledArtifactHashes
        correlationToken = $token
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    }
    $provenance | ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $resultsDirectory 'provenance.json')

    Write-Host "Compatibility E2E passed for $token."
}
catch {
    if ($environmentStarted) {
        & docker compose --project-name $projectName --file $composePath logs --no-color *>&1 |
            Set-Content -LiteralPath (Join-Path $resultsDirectory 'docker-compose.log')
    }
    throw
}
finally {
    if ($environmentStarted -and -not $KeepEnvironment) {
        & docker compose --project-name $projectName --file $composePath down --volumes --remove-orphans
    }
    $env:DOCKER_BUILDKIT = $previousBuildKit
    $env:COMPAT_FUNCTION_IMAGE = $previousFunctionImage
    $env:COMPAT_KAFKA_IMAGE = $previousKafkaImage
    foreach ($secretPath in $generatedSecretPaths) {
        if (Test-Path -LiteralPath $secretPath) {
            Remove-Item -LiteralPath $secretPath -Force
        }
    }
}
