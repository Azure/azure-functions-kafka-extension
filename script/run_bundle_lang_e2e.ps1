param(
    [string]$ExtensionBundleVersion = "4.37.0",
    [ValidateSet("Staging", "Production")]
    [string]$BundleSource = "Staging",
    [string]$ExtensionBundleSourceUri,
    [string]$Configuration = "Release",
    [string]$ResultsDirectory = "artifacts/LangE2E",
    [string]$TestCaseFilter = "FullyQualifiedName~ConfluentAppTest",
    [switch]$SkipImageBuild,
    [switch]$SkipInfrastructureStart,
    [switch]$KeepInfrastructureRunning
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$testProject = Join-Path $repoRoot "test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.csproj"
$testOutputDirectory = Join-Path $repoRoot "test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/bin/$Configuration/net8.0"
$testAssembly = Join-Path $testOutputDirectory "Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.dll"
$serverDirectory = Join-Path $repoRoot "test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/server"
$extensionBundleSourceUriWasSpecified = $PSBoundParameters.ContainsKey("ExtensionBundleSourceUri")

if ([System.IO.Path]::IsPathRooted($ResultsDirectory)) {
    $resolvedResultsDirectory = $ResultsDirectory
}
else {
    $resolvedResultsDirectory = Join-Path $repoRoot $ResultsDirectory
}

$safeBundleVersion = $ExtensionBundleVersion -replace '[^A-Za-z0-9_.-]', '_'
$trxFileName = "confluent-bundle-$safeBundleVersion.trx"
$azuriteAccountName = "devstoreaccount1"
<#[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Well known public Azurite emulator key. Used for local testing only.")]#>
$azuriteAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="

function Require-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function Invoke-NativeCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [switch]$IgnoreExitCode
    )

    Write-Host "> $FilePath $($Arguments -join ' ')"
    & $FilePath @Arguments
    $exitCode = $LASTEXITCODE
    if (-not $IgnoreExitCode -and $exitCode -ne 0) {
        throw "Command '$FilePath' failed with exit code $exitCode."
    }
}

function Test-DockerComposePlugin {
    & docker compose version *> $null
    return $LASTEXITCODE -eq 0
}

function Invoke-DockerCompose {
    param([string[]]$Arguments)

    if ($script:UseDockerComposePlugin) {
        Invoke-NativeCommand docker (@("compose") + $Arguments)
    }
    else {
        Invoke-NativeCommand docker-compose $Arguments
    }
}

function Set-ProcessEnvironmentVariable {
    param(
        [string]$Name,
        [AllowNull()]$Value
    )

    if ($null -eq $Value -or ($Value -is [string] -and $Value.Length -eq 0)) {
        Remove-Item "Env:$Name" -ErrorAction SilentlyContinue
    }
    else {
        Set-Item "Env:$Name" $Value
    }
}

function New-AzuriteConnectionString {
    param([string]$HostName)

    return "DefaultEndpointsProtocol=http;AccountName=$azuriteAccountName;AccountKey=$azuriteAccountKey;BlobEndpoint=http://${HostName}:10000/$azuriteAccountName;QueueEndpoint=http://${HostName}:10001/$azuriteAccountName;TableEndpoint=http://${HostName}:10002/$azuriteAccountName;"
}

function Resolve-ExtensionBundleSourceUri {
    if ($extensionBundleSourceUriWasSpecified) {
        if ([string]::IsNullOrWhiteSpace($ExtensionBundleSourceUri)) {
            return $null
        }

        return $ExtensionBundleSourceUri
    }

    if ($BundleSource -eq "Staging") {
        return "https://cdn-staging.functions.azure.com/public"
    }

    return $null
}

function Restore-ProcessEnvironment {
    param([hashtable]$Values)

    foreach ($name in $Values.Keys) {
        Set-ProcessEnvironmentVariable $name $Values[$name]
    }
}

Require-Command dotnet
Require-Command docker

if (Get-Command docker-compose -ErrorAction SilentlyContinue) {
    $script:UseDockerComposePlugin = $false
}
elseif (Test-DockerComposePlugin) {
    $script:UseDockerComposePlugin = $true
}
else {
    throw "Neither 'docker-compose' nor 'docker compose' is available."
}

$environmentVariablesToRestore = @(
    "EXTENSION_SOURCE",
    "EXTENSION_BUNDLE_VERSION",
    "FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI",
    "AzureWebJobsStorage",
    "AzureStorageQueueTestConnection",
    "ConfluentBrokerList",
    "EnableEventHubsTestsFlag"
)

$previousEnvironment = @{}
foreach ($name in $environmentVariablesToRestore) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

$infrastructureStartedByScript = $false

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path $resolvedResultsDirectory -Force | Out-Null

    if (-not $SkipImageBuild) {
        $packageDirectory = Join-Path $repoRoot "temp"
        if (Test-Path $packageDirectory) {
            Remove-Item $packageDirectory -Recurse -Force
        }

        Invoke-NativeCommand dotnet @(
            "pack",
            "-o",
            "temp",
            "--include-symbols",
            "src/Microsoft.Azure.WebJobs.Extensions.Kafka/Microsoft.Azure.WebJobs.Extensions.Kafka.csproj",
            "/p:Version=100.100.100-pre"
        )

        $functionAppImages = @(
            @{ Image = "azure-functions-kafka-java-confluent"; Dockerfile = "test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/java/Confluent/Dockerfile" },
            @{ Image = "azure-functions-kafka-python-confluent"; Dockerfile = "test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/python/Confluent/Dockerfile" },
            @{ Image = "azure-functions-kafka-javascript-confluent"; Dockerfile = "test/Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests/FunctionApps/javascript/Confluent/Dockerfile" }
        )

        foreach ($functionAppImage in $functionAppImages) {
            Invoke-NativeCommand docker @(
                "build",
                "--build-arg",
                "EXTENSION_SOURCE=bundle",
                "--build-arg",
                "EXTENSION_BUNDLE_VERSION=$ExtensionBundleVersion",
                "-f",
                $functionAppImage.Dockerfile,
                "-t",
                $functionAppImage.Image,
                "."
            )
        }
    }

    if (-not $SkipInfrastructureStart) {
        Push-Location $serverDirectory
        try {
            Invoke-DockerCompose @("up", "-d")
        }
        finally {
            Pop-Location
        }

        $infrastructureStartedByScript = $true
        Invoke-NativeCommand docker @("exec", "broker", "cub", "kafka-ready", "-b", "broker:29092", "1", "60")
    }

    Invoke-NativeCommand docker @("rm", "-f", "azure-functions-kafka-python-confluent", "azure-functions-kafka-java-confluent", "azure-functions-kafka-javascript-confluent") -IgnoreExitCode

    Set-ProcessEnvironmentVariable "EXTENSION_SOURCE" "bundle"
    Set-ProcessEnvironmentVariable "EXTENSION_BUNDLE_VERSION" $ExtensionBundleVersion
    $resolvedExtensionBundleSourceUri = Resolve-ExtensionBundleSourceUri
    if ([string]::IsNullOrWhiteSpace($resolvedExtensionBundleSourceUri)) {
        Set-ProcessEnvironmentVariable "FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI" $null
    }
    else {
        Set-ProcessEnvironmentVariable "FUNCTIONS_EXTENSIONBUNDLE_SOURCE_URI" $resolvedExtensionBundleSourceUri
    }

    Set-ProcessEnvironmentVariable "AzureWebJobsStorage" "UseDevelopmentStorage=true"
    Set-ProcessEnvironmentVariable "AzureStorageQueueTestConnection" (New-AzuriteConnectionString "127.0.0.1")
    Set-ProcessEnvironmentVariable "ConfluentBrokerList" "broker:29092"
    Set-ProcessEnvironmentVariable "EnableEventHubsTestsFlag" $null

    Invoke-NativeCommand dotnet @("build", $testProject, "--configuration", $Configuration)

    Invoke-NativeCommand dotnet @(
        "vstest",
        $testAssembly,
        "--TestCaseFilter:$TestCaseFilter",
        "--Logger:console;verbosity=normal",
        "--Logger:trx;LogFileName=$trxFileName",
        "--ResultsDirectory:$resolvedResultsDirectory"
    )

    Write-Host "Bundle Lang E2E sanity succeeded."
    Write-Host "Results: $(Join-Path $resolvedResultsDirectory $trxFileName)"
}
finally {
    Invoke-NativeCommand docker @("rm", "-f", "azure-functions-kafka-python-confluent", "azure-functions-kafka-java-confluent", "azure-functions-kafka-javascript-confluent") -IgnoreExitCode

    if ($infrastructureStartedByScript -and -not $KeepInfrastructureRunning) {
        Push-Location $serverDirectory
        try {
            Invoke-DockerCompose @("down", "-v")
        }
        finally {
            Pop-Location
        }
    }

    Restore-ProcessEnvironment $previousEnvironment
    Pop-Location
}