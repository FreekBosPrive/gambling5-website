# Deploy Azure Function App with Storage Account for Gigs API
# Estimated cost: ~$1-5/month (Consumption plan + minimal storage)

param(
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = "gambling5-rg",
    
    [Parameter(Mandatory = $false)]
    [string]$Location = "westeurope",
    
    [Parameter(Mandatory = $false)]
    [string]$FunctionAppName = "gambling5-api",
    
    [Parameter(Mandatory = $false)]
    [string]$StorageAccountName = "gambling5storage"
)

Write-Host "=== Deploying Gambling5 API ===" -ForegroundColor Cyan

# Ensure resource group exists
Write-Host "`n1. Checking resource group..." -ForegroundColor Yellow
$rgExists = az group exists --name $ResourceGroup
if ($rgExists -eq "false") {
    Write-Host "   Creating resource group $ResourceGroup..."
    az group create --name $ResourceGroup --location $Location
}
Write-Host "   Resource group ready." -ForegroundColor Green

# Create storage account (used for both Functions runtime and Table Storage)
Write-Host "`n2. Creating storage account..." -ForegroundColor Yellow
$storageExists = az storage account show --name $StorageAccountName --resource-group $ResourceGroup 2>$null
if (-not $storageExists) {
    az storage account create `
        --name $StorageAccountName `
        --resource-group $ResourceGroup `
        --location $Location `
        --sku Standard_LRS `
        --kind StorageV2 `
        --min-tls-version TLS1_2
    Write-Host "   Storage account created." -ForegroundColor Green
}
else {
    Write-Host "   Storage account already exists." -ForegroundColor Green
}

# Get storage connection string
$storageConnectionString = az storage account show-connection-string `
    --name $StorageAccountName `
    --resource-group $ResourceGroup `
    --query connectionString `
    --output tsv

# Create Function App (Consumption plan = pay per execution)
Write-Host "`n3. Creating Function App (Consumption plan)..." -ForegroundColor Yellow
$funcExists = az functionapp show --name $FunctionAppName --resource-group $ResourceGroup 2>$null
if (-not $funcExists) {
    az functionapp create `
        --name $FunctionAppName `
        --resource-group $ResourceGroup `
        --storage-account $StorageAccountName `
        --consumption-plan-location $Location `
        --runtime dotnet-isolated `
        --runtime-version 8 `
        --functions-version 4 `
        --os-type Windows
    Write-Host "   Function App created." -ForegroundColor Green
}
else {
    Write-Host "   Function App already exists." -ForegroundColor Green
}

# Configure CORS for the Static Web App
Write-Host "`n4. Configuring CORS..." -ForegroundColor Yellow
az functionapp cors add `
    --name $FunctionAppName `
    --resource-group $ResourceGroup `
    --allowed-origins "https://orange-smoke-088c08803.4.azurestaticapps.net" "https://www.gambling5.de" "http://localhost:5259"

Write-Host "   CORS configured." -ForegroundColor Green

# Output connection info
Write-Host "`n=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host "`nFunction App URL: https://$FunctionAppName.azurewebsites.net" -ForegroundColor Green
Write-Host "API Endpoint: https://$FunctionAppName.azurewebsites.net/api/gigs" -ForegroundColor Green

Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Deploy the function code:"
Write-Host "   cd Gambling5.Api"
Write-Host "   func azure functionapp publish $FunctionAppName"
Write-Host ""
Write-Host "2. Seed initial data:"
Write-Host "   ./seed-gigs.ps1 -FunctionUrl 'https://$FunctionAppName.azurewebsites.net/api'"
Write-Host ""
Write-Host "3. Update appsettings.Production.json with the API URL"
