# Build and Deploy Script for Gambling5 Website
# Usage:
#   ./build-and-deploy.ps1
#
# This script builds the Blazor WebAssembly site and deploys it to Azure Static Web Apps.
# It ensures the output is always in the correct publish folder and structure.

$ErrorActionPreference = 'Stop'

# Paths
$projectRoot = "$PSScriptRoot"
$webProject = Join-Path $projectRoot 'Gambling5.Web'
$publishDir = Join-Path $projectRoot 'publish/wwwroot'

# Clean previous publish output
if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

# Build and publish the Blazor WebAssembly project
Write-Host "Publishing Blazor WebAssembly project..."
dotnet publish "$webProject/Gambling5.Web.csproj" -c Release -o $publishDir/wwwroot

# Move files up if nested wwwroot exists
$nestedWwwroot = Join-Path $publishDir 'wwwroot'
if (Test-Path $nestedWwwroot) {
    Write-Host "Flattening nested wwwroot..."
    Copy-Item -Path "$nestedWwwroot\*" -Destination $publishDir -Recurse -Force
    Remove-Item -Recurse -Force $nestedWwwroot
}

Write-Host "Build complete. Output in: $publishDir"

# Deploy to Azure Static Web Apps (requires Azure CLI and SWA CLI)
$token = az staticwebapp secrets list --name gambling5-web --query "properties.apiKey" -o tsv
swa deploy "$publishDir" --deployment-token $token --env production

Write-Host "Deployment complete."
