# Store GitHub OAuth credentials in dotnet user-secrets for IntercomService (survives publish-debug /MIR).
# Usage:
#   .\scripts\intercom\set-intercom-github-user-secrets.ps1 -ClientId "<id>" -ClientSecret "<secret>"
#   .\scripts\intercom\set-intercom-github-user-secrets.ps1 -FromLocalFile   # migrate appsettings.Development.local.json once
[CmdletBinding()]
param(
    [string] $ClientId = "",
    [string] $ClientSecret = "",
    [switch] $FromLocalFile
)

$ErrorActionPreference = "Stop"
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\.."))
$proj = Join-Path $repoRoot "host\intercom-service\src\IntercomService\IntercomService.csproj"
if (-not (Test-Path -LiteralPath $proj)) {
    Write-Error "IntercomService.csproj not found: $proj"
    exit 1
}

if ($FromLocalFile) {
    $local = Join-Path $repoRoot "host\intercom-service\src\IntercomService\appsettings.Development.local.json"
    if (-not (Test-Path -LiteralPath $local)) {
        Write-Error "No appsettings.Development.local.json at $local"
        exit 1
    }
    $json = Get-Content -LiteralPath $local -Raw | ConvertFrom-Json
    $ClientId = $json.GitHub.ClientId
    $ClientSecret = $json.GitHub.ClientSecret
}

if ([string]::IsNullOrWhiteSpace($ClientId) -or [string]::IsNullOrWhiteSpace($ClientSecret)) {
    Write-Error "Provide -ClientId and -ClientSecret, or -FromLocalFile."
    exit 1
}

dotnet user-secrets set "GitHub:ClientId" $ClientId.Trim() --project $proj
dotnet user-secrets set "GitHub:ClientSecret" $ClientSecret.Trim() --project $proj
Write-Host "OK: GitHub OAuth in user-secrets (UserSecretsId 7f3c8a2e-4b1d-4f9a-9c2e-1a8b4d6e0f31)."
Write-Host "Path: $env:APPDATA\Microsoft\UserSecrets\7f3c8a2e-4b1d-4f9a-9c2e-1a8b4d6e0f31\secrets.json"
Write-Host "Works for dotnet run and published IntercomService.exe when ASPNETCORE_ENVIRONMENT=Development."
