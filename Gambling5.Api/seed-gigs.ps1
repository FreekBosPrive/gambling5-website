# Seed script for initial gigs data
# Run this after the Azure Function is deployed and Table Storage is created

param(
    [Parameter(Mandatory = $false)]
    [string]$FunctionUrl = "http://localhost:7071/api",
    
    [Parameter(Mandatory = $false)]
    [string]$FunctionKey = ""
)

$gigs = @(
    @{
        id          = "gig-001"
        date        = "2026-03-15T19:00:00"
        title       = "Frühjahrs Gala"
        venue       = "Private Veranstaltung - München"
        status      = "confirmed"
        description = "Exklusive Frühjahrsfeier"
        isPublic    = $true
    },
    @{
        id          = "gig-002"
        date        = "2026-04-07T20:00:00"
        title       = "Apres Ski Party"
        venue       = "Berghütte - Garmisch"
        status      = "confirmed"
        description = "Après-Ski Party mit Live-Musik"
        isPublic    = $true
    },
    @{
        id          = "gig-003"
        date        = "2026-05-20T18:00:00"
        title       = "Sommer Festival"
        venue       = "Marktplatz - Beuerberg"
        status      = "public"
        description = "Öffentliches Sommerfest"
        isPublic    = $true
    }
)

$headers = @{
    "Content-Type" = "application/json"
}

if ($FunctionKey) {
    $headers["x-functions-key"] = $FunctionKey
}

foreach ($gig in $gigs) {
    $body = $gig | ConvertTo-Json
    
    Write-Host "Creating gig: $($gig.title)..." -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$FunctionUrl/gigs" -Method Post -Headers $headers -Body $body
        Write-Host "  Created: $($response.id)" -ForegroundColor Green
    }
    catch {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nDone! Seeded $($gigs.Count) gigs." -ForegroundColor Green
