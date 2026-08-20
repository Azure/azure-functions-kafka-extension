[CmdletBinding()]
param(
    [string] $ManifestPath = (Join-Path $PSScriptRoot 'manifests/local-java.json'),
    [string] $DiagnosticsDirectory,
    [switch] $KeepEnvironment
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$runId = "compat-$([Guid]::NewGuid().ToString('N'))"
$runStartedAt = [DateTimeOffset]::UtcNow
$projectName = "kafka-compat-$($runId.Substring($runId.Length - 12))"
$composePath = Join-Path $PSScriptRoot 'docker-compose.yml'
$workingDirectory = Join-Path $repositoryRoot 'temp\compat'
$nugetDirectory = Join-Path $workingDirectory 'nuget'
$resultsRoot = if ([string]::IsNullOrWhiteSpace($DiagnosticsDirectory)) {
    Join-Path $workingDirectory 'results'
}
else {
    [IO.Path]::GetFullPath($DiagnosticsDirectory)
}
$resultsDirectory = Join-Path $resultsRoot $runId
$logsDirectory = Join-Path $resultsDirectory 'logs'
$stateDirectory = Join-Path $resultsDirectory 'state'
$functionMetadataDirectory = Join-Path $stateDirectory 'function-app'
$timelinePath = Join-Path $resultsDirectory 'timeline.jsonl'
$secretsDirectory = Join-Path $workingDirectory 'secrets'
$functionImage = "azure-functions-kafka-compat-java:$($runId.Substring($runId.Length - 12))"
$environmentStarted = $false
$generatedSecretPaths = [System.Collections.Generic.List[string]]::new()
$diagnosticFailures = [System.Collections.Generic.List[string]]::new()
$cleanupFailures = [System.Collections.Generic.List[string]]::new()
$assembledArtifactHashes = [ordered]@{}
$functionImageId = $null
$correlationToken = $null
$currentPhase = 'initialization'
$phaseStartedAt = $runStartedAt
$runStatus = 'failed'
$failureMessage = $null

function Write-TimelineEvent {
    param(
        [Parameter(Mandatory)]
        [string] $Event,
        [hashtable] $Data = @{}
    )

    $record = [ordered]@{
        timestampUtc = [DateTimeOffset]::UtcNow.ToString('O')
        runId = $runId
        phase = $currentPhase
        event = $Event
        data = $Data
    }
    Add-Content -LiteralPath $timelinePath -Value ($record | ConvertTo-Json -Compress -Depth 6)
}

function Start-Phase {
    param([Parameter(Mandatory)][string] $Name)

    $script:currentPhase = $Name
    $script:phaseStartedAt = [DateTimeOffset]::UtcNow
    Write-TimelineEvent -Event 'started'
}

function Complete-Phase {
    Write-TimelineEvent -Event 'completed' -Data @{
        durationMilliseconds = [math]::Round(
            ([DateTimeOffset]::UtcNow - $phaseStartedAt).TotalMilliseconds)
    }
}

function Invoke-Native {
    param(
        [Parameter(Mandatory, Position = 0)]
        [string] $Command,
        [Parameter(Position = 1, ValueFromRemainingArguments)]
        [string[]] $Arguments,
        [Parameter()]
        [string] $LogPath
    )

    if ([string]::IsNullOrWhiteSpace($LogPath)) {
        & $Command @Arguments
    }
    else {
        & $Command @Arguments 2>&1 | Tee-Object -FilePath $LogPath -Append
    }
    $exitCode = $LASTEXITCODE
    Write-TimelineEvent -Event 'command' -Data @{
        command = $Command
        exitCode = $exitCode
        log = if ($LogPath) { [IO.Path]::GetRelativePath($resultsDirectory, $LogPath) } else { $null }
    }
    if ($exitCode -ne 0) {
        throw "$Command failed with exit code $exitCode."
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
    if ($content -notmatch [Regex]::Escape($manifest.packageFeeds.nuget)) {
        throw 'CFS_NUGET_CONFIG does not contain the manifest-selected CFS NuGet source.'
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
        $mirrorUrl -ne $manifest.packageFeeds.maven) {
        throw 'CFS_MAVEN_SETTINGS must mirror all repositories through the manifest-selected CFS feed.'
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
    if ($manifest.packageFeeds.policy -ne 'cfs-only') {
        throw 'The Java compatibility harness requires packageFeeds.policy to be cfs-only.'
    }
    $expectedAzureDevOpsResource = '499b84ac-1321-427f-aa17-267ca6975798'
    $expectedNuGetFeed = 'https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/nuget/v3/index.json'
    $expectedMavenFeed = 'https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/maven/v1'
    if ($manifest.packageFeeds.azureDevOpsResource -ne $expectedAzureDevOpsResource -or
        $manifest.packageFeeds.nuget -ne $expectedNuGetFeed -or
        $manifest.packageFeeds.maven -ne $expectedMavenFeed) {
        throw 'Package feeds and the token resource must use the approved Azure Functions CFS endpoints.'
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
        --resource $manifest.packageFeeds.azureDevOpsResource `
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
    <add key="upstream-public" value="$($manifest.packageFeeds.nuget)" />
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
      <url>$($manifest.packageFeeds.maven)</url>
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

function Save-ComposeDiagnostics {
    if (-not $environmentStarted) {
        return
    }

    function Invoke-DiagnosticCapture {
        param(
            [Parameter(Mandatory)]
            [string] $Name,
            [Parameter(Mandatory)]
            [scriptblock] $Action
        )

        try {
            & $Action
            if ($LASTEXITCODE -ne 0) {
                throw "$Name exited with code $LASTEXITCODE."
            }
        }
        catch {
            $message = "$Name`: $($_.Exception.Message)"
            $diagnosticFailures.Add($message)
            Write-TimelineEvent -Event 'diagnostic-command-failed' -Data @{
                command = $Name
                message = $_.Exception.Message
            }
        }
    }

    foreach ($service in @('function', 'kafka')) {
        Invoke-DiagnosticCapture -Name "compose logs $service" -Action {
            & docker compose --project-name $projectName --file $composePath `
                logs --timestamps --no-color $service *>&1 |
                Set-Content -LiteralPath (Join-Path $logsDirectory "$service.log")
        }
    }

    Invoke-DiagnosticCapture -Name 'compose ps' -Action {
        & docker compose --project-name $projectName --file $composePath `
            ps --all --format json *>&1 |
            Set-Content -LiteralPath (Join-Path $stateDirectory 'compose-ps.jsonl')
    }

    $script:diagnosticContainerIds = @()
    Invoke-DiagnosticCapture -Name 'resolve compose containers' -Action {
        $script:diagnosticContainerIds = @(
            & docker compose --project-name $projectName --file $composePath ps --all --quiet
        )
    }
    $containerIds = $script:diagnosticContainerIds
    if ($containerIds.Count -gt 0) {
        $containerIds = @($containerIds | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        Invoke-DiagnosticCapture -Name 'inspect compose containers' -Action {
            & docker inspect @containerIds |
                Set-Content -LiteralPath (Join-Path $stateDirectory 'container-inspect.json')
        }
    }

    Invoke-DiagnosticCapture -Name 'inspect function image' -Action {
        & docker image inspect $functionImage |
            Set-Content -LiteralPath (Join-Path $stateDirectory 'function-image-inspect.json')
    }

    $script:diagnosticFunctionContainerId = ''
    Invoke-DiagnosticCapture -Name 'resolve function container' -Action {
        $script:diagnosticFunctionContainerId = (& docker compose --project-name $projectName --file $composePath `
            ps --all --quiet function).Trim()
    }
    $functionContainerId = $script:diagnosticFunctionContainerId
    if (-not [string]::IsNullOrWhiteSpace($functionContainerId)) {
        New-Item -ItemType Directory -Path $functionMetadataDirectory -Force | Out-Null
        Invoke-DiagnosticCapture -Name 'copy host metadata' -Action {
            & docker cp "${functionContainerId}:/home/site/wwwroot/host.json" `
                (Join-Path $functionMetadataDirectory 'host.json')
        }
        Invoke-DiagnosticCapture -Name 'copy extension metadata' -Action {
            & docker cp "${functionContainerId}:/home/site/wwwroot/bin/extensions.json" `
                (Join-Path $functionMetadataDirectory 'extensions.json')
        }
        Invoke-DiagnosticCapture -Name 'capture function metadata' -Action {
            & docker exec $functionContainerId sh -c `
                'find /home/site/wwwroot -maxdepth 2 -name function.json -print -exec cat {} \;' *>&1 |
                Set-Content -LiteralPath (Join-Path $functionMetadataDirectory 'functions.txt')
        }
        Invoke-DiagnosticCapture -Name 'capture Java worker layout' -Action {
            & docker exec $functionContainerId sh -c `
                'find /azure-functions-host/workers/java -maxdepth 2 -type f -printf "%p %s bytes\n" | sort' *>&1 |
                Set-Content -LiteralPath (Join-Path $stateDirectory 'java-worker-layout.txt')
        }
    }

    if ($diagnosticFailures.Count -eq 0) {
        Write-TimelineEvent -Event 'diagnostics-captured'
    }
    else {
        Write-TimelineEvent -Event 'diagnostics-failed' -Data @{
            failures = @($diagnosticFailures)
        }
    }
}

function Write-RunSummary {
    $summary = [ordered]@{
        runId = $runId
        status = $runStatus
        failedPhase = if ($runStatus -eq 'failed') { $currentPhase } else { $null }
        error = $failureMessage
        manifest = $manifest.name
        correlationToken = $correlationToken
        startedAtUtc = $runStartedAt.ToString('O')
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
        durationSeconds = [math]::Round(
            ([DateTimeOffset]::UtcNow - $runStartedAt).TotalSeconds, 3)
        diagnostics = [ordered]@{
            timeline = 'timeline.jsonl'
            logs = 'logs'
            state = 'state'
            captureStatus = if ($diagnosticFailures.Count -eq 0) { 'complete' } else { 'partial' }
            captureFailures = @($diagnosticFailures)
            cleanupFailures = @($cleanupFailures)
            provenance = if (Test-Path (Join-Path $resultsDirectory 'provenance.json')) {
                'provenance.json'
            }
            else {
                $null
            }
        }
    }
    $summary | ConvertTo-Json -Depth 6 |
        Set-Content -LiteralPath (Join-Path $resultsDirectory 'summary.json')
}

function Assert-Toolchains {
    $toolchainLog = Join-Path $logsDirectory 'toolchains.log'
    $additionsInfo = (& docker run --rm --entrypoint mvn `
        $manifest.buildTooling.javaAdditionsImage --version 2>&1) -join [Environment]::NewLine
    $workerInfo = (& docker run --rm --entrypoint mvn `
        $manifest.buildTooling.javaImage --version 2>&1) -join [Environment]::NewLine
    $dotnetVersion = (& docker run --rm --entrypoint dotnet `
        $manifest.buildTooling.dotnetSdkImage --version 2>&1) -join [Environment]::NewLine
    @($additionsInfo, '', $workerInfo, '', "dotnet SDK $dotnetVersion") |
        Set-Content -LiteralPath $toolchainLog

    $additionsJavaPattern = "Java version:\s+(?:1\.)?$([Regex]::Escape($manifest.toolchains.javaAdditions))(?:[.,]|\s)"
    $workerJavaPattern = "Java version:\s+$([Regex]::Escape($manifest.toolchains.javaWorker))(?:[.,]|\s)"
    if ($additionsInfo -notmatch "Apache Maven $([Regex]::Escape($manifest.toolchains.mavenAdditions))" -or
        $additionsInfo -notmatch $additionsJavaPattern) {
        throw 'The Java additions image does not match its manifest-declared Maven/JDK toolchain.'
    }
    if ($workerInfo -notmatch "Apache Maven $([Regex]::Escape($manifest.toolchains.mavenWorker))" -or
        $workerInfo -notmatch $workerJavaPattern) {
        throw 'The Java worker image does not match its manifest-declared Maven/JDK toolchain.'
    }
    if (-not $dotnetVersion.Trim().StartsWith($manifest.toolchains.dotnetSdk)) {
        throw 'The .NET SDK image does not match its manifest-declared toolchain.'
    }
    if ([int]$manifest.functionApp.compilerRelease -gt [int]$manifest.toolchains.javaWorker) {
        throw 'The Function App compiler release cannot exceed the Java worker build JDK.'
    }
    if ($manifest.functionApp.javaVersion -ne $manifest.toolchains.javaWorker) {
        throw 'The Function App runtime Java version must match the assembled Java worker runtime.'
    }
}

$head = $null
$packageVersion = $null
$packagePath = $null
$projectPath = Join-Path $repositoryRoot 'src\Microsoft.Azure.WebJobs.Extensions.Kafka\Microsoft.Azure.WebJobs.Extensions.Kafka.csproj'
$previousBuildKit = $env:DOCKER_BUILDKIT
$previousFunctionImage = $env:COMPAT_FUNCTION_IMAGE
$previousKafkaImage = $env:COMPAT_KAFKA_IMAGE

New-Item -ItemType Directory -Path $nugetDirectory, $resultsDirectory, $logsDirectory, $stateDirectory -Force | Out-Null
Copy-Item -LiteralPath $ManifestPath -Destination (Join-Path $resultsDirectory 'manifest.json')
Write-TimelineEvent -Event 'run-created' -Data @{ manifestPath = $ManifestPath }

try {
    Start-Phase -Name 'preflight'
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
    $cfs = New-CfsSecrets
    Assert-CfsConfiguration -Path $cfs.NuGetPath
    Assert-CfsMavenSettings -Path $cfs.MavenPath
    Assert-Toolchains
    Complete-Phase

    Start-Phase -Name 'pack-extension'
    $extensionBuildLog = Join-Path $logsDirectory 'extension-build.log'
    Invoke-Native dotnet restore $projectPath --configfile $cfs.NuGetPath -LogPath $extensionBuildLog
    Invoke-Native dotnet pack $projectPath --output $nugetDirectory --include-symbols --no-restore `
        "/p:Version=$packageVersion" -LogPath $extensionBuildLog
    $packagePath = Get-ChildItem -LiteralPath $nugetDirectory -Filter "Microsoft.Azure.WebJobs.Extensions.Kafka.$packageVersion.nupkg" |
        Select-Object -First 1
    if ($null -eq $packagePath) {
        throw "Kafka extension package $packageVersion was not produced."
    }
    $packageContextPath = [IO.Path]::GetRelativePath($repositoryRoot, $packagePath.FullName).Replace('\', '/')
    Complete-Phase

    Start-Phase -Name 'build-runtime-image'
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
        --build-arg "FUNCTION_APP_NAME=$($manifest.functionApp.name)" `
        --build-arg "FUNCTION_JAVA_VERSION=$($manifest.functionApp.javaVersion)" `
        --build-arg "FUNCTION_COMPILER_RELEASE=$($manifest.functionApp.compilerRelease)" `
        --build-arg "AZURE_FUNCTIONS_MAVEN_PLUGIN_VERSION=$($manifest.functionApp.azureFunctionsMavenPluginVersion)" `
        --build-arg "MAVEN_COMPILER_PLUGIN_VERSION=$($manifest.functionApp.mavenCompilerPluginVersion)" `
        --build-arg "JAVA_WORKER_REPOSITORY=$($manifest.worker.repository)" `
        --build-arg "JAVA_WORKER_COMMIT=$($manifest.worker.commit)" `
        --build-arg "KAFKA_EXTENSION_VERSION=$packageVersion" `
        --build-arg "KAFKA_NUPKG=$packageContextPath" `
        --file (Join-Path $PSScriptRoot 'java\Dockerfile') `
        --tag $functionImage `
        $repositoryRoot `
        -LogPath (Join-Path $logsDirectory 'docker-build.log')
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
    Complete-Phase

    Start-Phase -Name 'start-kafka'
    $env:COMPAT_FUNCTION_IMAGE = $functionImage
    $env:COMPAT_KAFKA_IMAGE = $manifest.broker.image
    $environmentStarted = $true
    Invoke-Native docker compose --project-name $projectName --file $composePath up --detach kafka `
        -LogPath (Join-Path $logsDirectory 'orchestrator.log')

    Invoke-Native docker compose --project-name $projectName --file $composePath exec --no-TTY kafka `
        kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists `
        --topic compat-input --partitions 1 --replication-factor 1 `
        -LogPath (Join-Path $logsDirectory 'orchestrator.log')
    Invoke-Native docker compose --project-name $projectName --file $composePath exec --no-TTY kafka `
        kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists `
        --topic compat-result --partitions 1 --replication-factor 1 `
        -LogPath (Join-Path $logsDirectory 'orchestrator.log')
    Complete-Phase

    Start-Phase -Name 'start-function-host'
    Invoke-Native docker compose --project-name $projectName --file $composePath up --detach function `
        -LogPath (Join-Path $logsDirectory 'orchestrator.log')
    Complete-Phase

    Start-Phase -Name 'invoke-function'
    $correlationToken = $runId
    $deadline = [DateTimeOffset]::UtcNow.AddMinutes(3)
    $invoked = $false
    while ([DateTimeOffset]::UtcNow -lt $deadline -and -not $invoked) {
        try {
            $response = Invoke-WebRequest `
                -Uri 'http://localhost:7071/api/ProduceCompatibilityMessage' `
                -Method Post `
                -Body $correlationToken `
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
    Write-TimelineEvent -Event 'http-accepted' -Data @{ correlationToken = $correlationToken }
    Complete-Phase

    Start-Phase -Name 'verify-kafka-result'
    $result = & docker compose --project-name $projectName --file $composePath exec --no-TTY kafka `
        kafka-console-consumer --bootstrap-server kafka:29092 --topic compat-result `
        --from-beginning --max-messages 1 --timeout-ms 60000 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Kafka result consumer failed: $($result -join [Environment]::NewLine)"
    }
    $resultText = $result -join [Environment]::NewLine
    Set-Content -LiteralPath (Join-Path $logsDirectory 'verifier.log') -Value $resultText
    if ($resultText -notmatch [Regex]::Escape($correlationToken)) {
        throw "The result topic did not contain correlation token $correlationToken."
    }
    Complete-Phase

    Start-Phase -Name 'write-provenance'
    $provenance = [ordered]@{
        runId = $runId
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
        functionApp = $manifest.functionApp
        packageFeeds = [ordered]@{
            policy = $manifest.packageFeeds.policy
            nuget = $manifest.packageFeeds.nuget
            maven = $manifest.packageFeeds.maven
        }
        toolchains = $manifest.toolchains
        buildTooling = $manifest.buildTooling
        assembledImageId = $functionImageId
        assembledArtifactSha256 = $assembledArtifactHashes
        correlationToken = $correlationToken
        completedAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    }
    $provenance | ConvertTo-Json -Depth 8 |
        Set-Content -LiteralPath (Join-Path $resultsDirectory 'provenance.json')

    $runStatus = 'passed'
    Complete-Phase
    Write-Host "Compatibility E2E passed for $correlationToken. Diagnostics: $resultsDirectory"
}
catch {
    $failureMessage = $_.Exception.Message
    Write-TimelineEvent -Event 'failed' -Data @{ message = $failureMessage }
    throw
}
finally {
    try {
        Save-ComposeDiagnostics
    }
    catch {
        $diagnosticFailures.Add("diagnostics: $($_.Exception.Message)")
    }

    if ($environmentStarted -and -not $KeepEnvironment) {
        try {
            & docker compose --project-name $projectName --file $composePath down --volumes --remove-orphans
            if ($LASTEXITCODE -ne 0) {
                throw "docker compose down exited with code $LASTEXITCODE."
            }
        }
        catch {
            $cleanupFailures.Add("compose: $($_.Exception.Message)")
        }
    }

    if ($null -ne $functionImageId -and -not $KeepEnvironment) {
        try {
            & docker image rm $functionImage
            if ($LASTEXITCODE -ne 0) {
                throw "docker image rm exited with code $LASTEXITCODE."
            }
        }
        catch {
            $cleanupFailures.Add("image: $($_.Exception.Message)")
        }
    }

    $env:DOCKER_BUILDKIT = $previousBuildKit
    $env:COMPAT_FUNCTION_IMAGE = $previousFunctionImage
    $env:COMPAT_KAFKA_IMAGE = $previousKafkaImage
    foreach ($secretPath in $generatedSecretPaths) {
        if (Test-Path -LiteralPath $secretPath) {
            try {
                Remove-Item -LiteralPath $secretPath -Force
            }
            catch {
                $cleanupFailures.Add("secret $secretPath`: $($_.Exception.Message)")
            }
        }
    }

    if ($cleanupFailures.Count -gt 0) {
        try {
            Write-TimelineEvent -Event 'cleanup-failed' -Data @{ failures = @($cleanupFailures) }
        }
        catch {
            $diagnosticFailures.Add("cleanup timeline: $($_.Exception.Message)")
        }
    }

    try {
        Write-RunSummary
    }
    catch {
        Write-Warning "Unable to write compatibility run summary: $($_.Exception.Message)"
    }
}
