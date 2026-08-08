<#
.SYNOPSIS
    Runs the ATS Resume Builder app locally. It's a single self-contained
    static HTML file (no build step, no dependencies), so this just serves
    it over http://localhost and opens it in your default browser.

.PARAMETER Port
    Port to serve on (default 8081).

.EXAMPLE
    ./scripts/start.ps1
#>
param(
    [int]$Port = 8081
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$entry = "index.html"

if (-not (Test-Path (Join-Path $root $entry))) {
    Write-Error "No index.html found in $root"
    exit 1
}

$mimeMap = @{
    ".html" = "text/html"; ".css" = "text/css"; ".js" = "application/javascript"
    ".json" = "application/json"; ".png" = "image/png"; ".jpg" = "image/jpeg"
    ".jpeg" = "image/jpeg"; ".svg" = "image/svg+xml"; ".ico" = "image/x-icon"
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()

Write-Host "ATS Resume Builder running at http://localhost:$Port/$entry" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop." -ForegroundColor DarkGray
Start-Process "http://localhost:$Port/$entry"

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $reqPath = $context.Request.Url.LocalPath.TrimStart('/')
        if ([string]::IsNullOrEmpty($reqPath)) { $reqPath = $entry }
        $filePath = Join-Path $root $reqPath

        if (Test-Path $filePath -PathType Leaf) {
            $ext = [System.IO.Path]::GetExtension($filePath)
            $contentType = $mimeMap[$ext]
            if (-not $contentType) { $contentType = "application/octet-stream" }
            $bytes = [System.IO.File]::ReadAllBytes($filePath)
            $context.Response.ContentType = $contentType
            $context.Response.ContentLength64 = $bytes.Length
            $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        } else {
            $context.Response.StatusCode = 404
        }
        $context.Response.OutputStream.Close()
    }
} finally {
    $listener.Stop()
}
