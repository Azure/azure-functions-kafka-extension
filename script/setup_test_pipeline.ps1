param
(
    [Switch]
    $UseCoreToolsBuildFromIntegrationTests
)

$FUNC_RUNTIME_VERSION = '3'
$arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$os = if ($IsWindows) { "win" } else { if ($IsMacOS) { "osx" } else { "linux" } }

$env:CORE_TOOLS_URL = $null
$coreToolsUrl = $null
if ($UseCoreToolsBuildFromIntegrationTests.IsPresent)
{
    Write-Host "Install the Core Tools for Integration Tests..."
    $env:CORE_TOOLS_URL = "https://functionsintegclibuilds.blob.core.windows.net/builds/$FUNC_RUNTIME_VERSION/latest/Azure.Functions.Cli.$os-$arch.zip"
    $coreToolsUrl = "https://functionsintegclibuilds.blob.core.windows.net/builds/$FUNC_RUNTIME_VERSION/latest"
}
else
{
    Write-Host "Install the Core Tools..."
    $env:CORE_TOOLS_URL = "https://functionsclibuilds.blob.core.windows.net/builds/$FUNC_RUNTIME_VERSION/latest/Azure.Functions.Cli.$os-$arch.zip"
    $coreToolsUrl = "https://functionsclibuilds.blob.core.windows.net/builds/$FUNC_RUNTIME_VERSION/latest"
}

$FUNC_CLI_DIRECTORY = Join-Path $(Agent.BuildDirectory) 'Azure.Functions.Cli'
Write-Host $FUNC_CLI_DIRECTORY

Write-Host 'Deleting the Core Tools if exists...'
Remove-Item -Force "$FUNC_CLI_DIRECTORY.zip" -ErrorAction Ignore
Remove-Item -Recurse -Force $FUNC_CLI_DIRECTORY -ErrorAction Ignore

$version = Invoke-RestMethod -Uri "$coreToolsUrl/version.txt"
Write-Host "Downloading the Core Tools (Version: $version)..."

$output = "$FUNC_CLI_DIRECTORY.zip"
Write-Host "Downloading the Core Tools from url: $env:CORE_TOOLS_URL"
Invoke-RestMethod -Uri $env:CORE_TOOLS_URL -OutFile $output

Write-Host 'Extracting Core Tools...'
Expand-Archive $output -DestinationPath $FUNC_CLI_DIRECTORY 

$funcExePath = Join-Path $FUNC_CLI_DIRECTORY $FUNC_EXE_NAME

if ($IsMacOS -or $IsLinux) {
    chmod +x $funcExePath
}

Write-Host "Setting Core tools in PATH"
Write-Host "##vso[task.setvariable variable=PATH;]${env:PATH};$FUNC_CLI_DIRECTORY";