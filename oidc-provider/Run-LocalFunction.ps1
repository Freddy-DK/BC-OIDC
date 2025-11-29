# Builds the solution, runs unit tests, then launches the Azure Functions host.
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$solutionPath = Join-Path $PSScriptRoot 'oidc-provider.sln'
$projectDir = Join-Path $PSScriptRoot 'src\OidcProvider'
$buildOutput = Join-Path $projectDir 'bin\Debug\net8.0'

Write-Host 'Building solution...' -ForegroundColor Cyan
& dotnet build $solutionPath

Write-Host 'Running tests...' -ForegroundColor Cyan
& dotnet test $solutionPath

if (-not (Test-Path $buildOutput))
{
    throw "Build output directory not found: $buildOutput"
}

Write-Host 'Starting Azure Functions host...' -ForegroundColor Cyan
Push-Location $projectDir
try
{
    & func start --dotnet-isolated --script-root $buildOutput
}
finally
{
    Pop-Location
}
