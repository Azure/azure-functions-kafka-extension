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
$functionImage = 'azure-functions-kafka-compat-java:local'
$environmentStarted = $false

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
    if ($manifest.qualificationMode -eq 'release' -and $manifest.worker.source -eq 'bundled') {
        throw 'Release qualification requires an independently identified worker artifact.'
    }
    if ($manifest.host.source -ne 'container' -or $manifest.host.image -notmatch '@sha256:[a-f0-9]{64}$') {
        throw 'The initial Host provider requires a digest-pinned container image.'
    }
    if ($manifest.kafkaExtension.source -ne 'workspace') {
        throw 'The initial Kafka extension provider supports only the current workspace.'
    }
    if ($manifest.broker.source -ne 'container' -or $manifest.broker.image -notmatch '@sha256:[a-f0-9]{64}$') {
        throw 'The Local Kafka provider requires a digest-pinned container image.'
    }
    if ($manifest.buildTooling.source -ne 'container' -or $manifest.buildTooling.image -notmatch '@sha256:[a-f0-9]{64}$') {
        throw 'Build tooling must use a digest-pinned container image.'
    }
}

Assert-CfsConfiguration -Path $env:CFS_NUGET_CONFIG
Assert-CfsMavenSettings -Path $env:CFS_MAVEN_SETTINGS
Assert-Manifest

$head = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to resolve the Kafka extension workspace commit.'
}
if ($manifest.kafkaExtension.ref -ne 'HEAD' -and -not $head.StartsWith($manifest.kafkaExtension.ref)) {
    throw "Workspace commit $head does not match requested ref $($manifest.kafkaExtension.ref)."
}

$packageVersion = "0.0.0-e2e.$($head.Substring(0, 12).ToLowerInvariant())"
$projectPath = Join-Path $repositoryRoot 'src\Microsoft.Azure.WebJobs.Extensions.Kafka\Microsoft.Azure.WebJobs.Extensions.Kafka.csproj'
$previousBuildKit = $env:DOCKER_BUILDKIT
$previousFunctionImage = $env:COMPAT_FUNCTION_IMAGE
$previousKafkaImage = $env:COMPAT_KAFKA_IMAGE

try {
    New-Item -ItemType Directory -Path $nugetDirectory, $resultsDirectory -Force | Out-Null

    Invoke-Native dotnet restore $projectPath --configfile $env:CFS_NUGET_CONFIG
    Invoke-Native dotnet pack $projectPath --output $nugetDirectory --include-symbols --no-restore "/p:Version=$packageVersion"

    $env:DOCKER_BUILDKIT = '1'
    Invoke-Native docker build `
        --secret "id=nuget_config,src=$($env:CFS_NUGET_CONFIG)" `
        --secret "id=maven_settings,src=$($env:CFS_MAVEN_SETTINGS)" `
        --build-arg "DOTNET_SDK_IMAGE=$($manifest.buildTooling.image)" `
        --build-arg "FUNCTIONS_JAVA_IMAGE=$($manifest.host.image)" `
        --build-arg "JAVA_LIBRARY_VERSION=$($manifest.languageLibrary.version)" `
        --build-arg "KAFKA_EXTENSION_VERSION=$packageVersion" `
        --file (Join-Path $PSScriptRoot 'java\Dockerfile') `
        --tag $functionImage `
        $repositoryRoot

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
        }
        host = $manifest.host
        worker = $manifest.worker
        languageLibrary = $manifest.languageLibrary
        broker = $manifest.broker
        buildTooling = $manifest.buildTooling
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
}
