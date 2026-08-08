<#
.SYNOPSIS
    Stops the Docker infrastructure (Postgres + Redis) for Job Search Aggregator.
    The API and frontend windows opened by start.ps1 run in their own
    PowerShell windows and must be stopped there (Ctrl+C or close the window).

.PARAMETER RemoveVolumes
    Also delete the Postgres/Redis data volumes (full reset - next start will
    re-run migrations against an empty database).

.EXAMPLE
    ./scripts/stop.ps1
.EXAMPLE
    ./scripts/stop.ps1 -RemoveVolumes
#>
param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "==> Stopping Docker services..." -ForegroundColor Cyan
if ($RemoveVolumes) {
    docker compose -f "$root\docker\docker-compose.yml" down -v
    Write-Host "Postgres/Redis containers and volumes removed (data wiped)." -ForegroundColor Yellow
} else {
    docker compose -f "$root\docker\docker-compose.yml" down
    Write-Host "Postgres/Redis containers stopped (data preserved in volumes)." -ForegroundColor Green
}

Write-Host ""
Write-Host "Note: close/Ctrl+C the API and frontend PowerShell windows manually if still running." -ForegroundColor DarkGray
