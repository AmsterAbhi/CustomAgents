<#
.SYNOPSIS
    Starts everything needed to run Job Search Aggregator locally:
    Postgres + Redis (Docker), the .NET API (which also hosts the background
    scheduler that runs job providers), and the Angular frontend.

.PARAMETER SkipMigration
    Skip running `dotnet ef database update` (use if the DB schema is already
    up to date and you want a faster startup).

.PARAMETER NoFrontend
    Only start Docker + the API, skip launching the Angular dev server.

.EXAMPLE
    ./scripts/start.ps1
.EXAMPLE
    ./scripts/start.ps1 -SkipMigration -NoFrontend
#>
param(
    [switch]$SkipMigration,
    [switch]$NoFrontend
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }

Write-Step "Starting Docker services (Postgres on 5433, Redis on 6379)..."
docker compose -f "$root\docker\docker-compose.yml" up -d

Write-Step "Waiting for Postgres to report healthy..."
$healthy = $false
for ($i = 0; $i -lt 30; $i++) {
    $status = docker inspect --format='{{.State.Health.Status}}' job-search-aggregator-postgres 2>$null
    if ($status -eq "healthy") { $healthy = $true; break }
    Start-Sleep -Seconds 2
}
if (-not $healthy) {
    Write-Warning "Postgres did not report healthy within ~60s - continuing anyway, but the API may fail to connect."
}

if (-not $SkipMigration) {
    Write-Step "Applying EF Core migrations (dotnet ef database update)..."
    dotnet ef database update `
        --project "$root\src\Infrastructure\JobSearchAggregator.Infrastructure.csproj" `
        --startup-project "$root\src\Api\JobSearchAggregator.Api.csproj"
}

Write-Step "Starting API in a new window (http://localhost:5071)..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root'; dotnet run --project src\Api\JobSearchAggregator.Api.csproj"

if (-not $NoFrontend) {
    Write-Step "Starting Angular frontend in a new window (http://localhost:4200)..."
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend'; npm start"
}

Write-Host ""
Write-Host "Services starting up:" -ForegroundColor Green
Write-Host "  API (Swagger UI): http://localhost:5071/swagger"
Write-Host "  API health check: http://localhost:5071/health"
Write-Host "  Frontend:         http://localhost:4200"
Write-Host "  Postgres:         localhost:5433 (db=job_search_aggregator, user=postgres, pw=postgres)"
Write-Host "  Redis:            localhost:6379"
Write-Host ""
Write-Host "Run .\scripts\stop.ps1 to stop the Docker services when done." -ForegroundColor DarkGray
