Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repositoryRoot 'src\Microsoft.Azure.WebJobs.Extensions.Kafka\Microsoft.Azure.WebJobs.Extensions.Kafka.csproj'
$nugetConfigPath = Join-Path $repositoryRoot 'NuGet.config'
$workingDirectory = Join-Path $repositoryRoot 'temp'

if ([string]::IsNullOrWhiteSpace($env:PIP_INDEX_URL)) {
    throw 'PIP_INDEX_URL must be set to the authenticated CFS Python feed.'
}

Push-Location $repositoryRoot
$previousDockerBuildKit = $env:DOCKER_BUILDKIT
try {
    if (Test-Path -LiteralPath $workingDirectory) {
        Remove-Item -LiteralPath $workingDirectory -Recurse -Force
    }

    $restoreArguments = @(
        'restore'
        $projectPath
        '--configfile'
        $nugetConfigPath
    )
    & dotnet @restoreArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    foreach ($packageVersion in @('100.100.100-pre', '4.0.0')) {
        $packArguments = @(
            'pack'
            $projectPath
            '--output'
            $workingDirectory
            '--include-symbols'
            '--no-restore'
            "/p:Version=$packageVersion"
        )
        & dotnet @packArguments
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet pack failed for version $packageVersion with exit code $LASTEXITCODE."
        }
    }

    $env:DOCKER_BUILDKIT = '1'
    $builds = @(
        @{
            Dockerfile = '.\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\EventHub\Dockerfile'
            Image = 'azure-functions-kafka-java-eventhub'
            Python = $false
        }
        @{
            Dockerfile = '.\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\EventHub\Dockerfile'
            Image = 'azure-functions-kafka-python-eventhub'
            Python = $true
        }
        @{
            Dockerfile = '.\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\java\Confluent\Dockerfile'
            Image = 'azure-functions-kafka-java-confluent'
            Python = $false
        }
        @{
            Dockerfile = '.\test\Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests\FunctionApps\python\Confluent\Dockerfile'
            Image = 'azure-functions-kafka-python-confluent'
            Python = $true
        }
    )

    foreach ($build in $builds) {
        $dockerArguments = @(
            'build'
            '--secret'
            "id=nuget_config,src=$nugetConfigPath"
        )
        if ($build.Python) {
            $dockerArguments += @('--secret', 'id=pip_index_url,env=PIP_INDEX_URL')
        }

        $dockerArguments += @('-f', $build.Dockerfile, '-t', $build.Image, '.')
        & docker @dockerArguments
        if ($LASTEXITCODE -ne 0) {
            throw "docker build for $($build.Image) failed with exit code $LASTEXITCODE."
        }
    }
}
finally {
    $env:DOCKER_BUILDKIT = $previousDockerBuildKit
    Pop-Location
}
