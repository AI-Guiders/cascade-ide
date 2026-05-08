# Generic dotnet publish to a fixed target path (no spaces-friendly).
# Supports killing a running app that would lock the target.
#
# Usage examples:
#   .\scripts\deploy\publish-to-fixed-target.ps1 -Project .\CascadeIDE.csproj -Runtime win-x64 -Configuration Debug -Target D:\cascade-ide-debug -SelfContained
#   .\scripts\deploy\publish-to-fixed-target.ps1 -Project .\MyApp\MyApp.csproj -Runtime win-x64 -Configuration Release -Target D:\myapp -KillRunning
#
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $Project,

    [string] $Runtime = "win-x64",
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Debug",

    [switch] $SelfContained,

    [string] $OutDir,
    [Parameter(Mandatory = $true)]
    [string] $Target,

    [string] $AppExeName,

    [switch] $KillRunning,

    [string[]] $MsbuildProps = @(),
    [string[]] $AdditionalDotnetArgs = @()
)

$ErrorActionPreference = "Stop"

function Resolve-FullPath([string] $p) {
    return [System.IO.Path]::GetFullPath($p)
}

function Get-ProjectExeName([string] $projectPath) {
    if (-not [string]::IsNullOrWhiteSpace($AppExeName)) { return $AppExeName }
    return [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
}

function Stop-AppIfRunning {
    param(
        [string] $ExpectedExePath,
        [string] $ProcessName
    )

    $procs = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if (-not $procs) { return }

    foreach ($p in $procs) {
        $exePath = $null
        try { $exePath = $p.Path } catch { $exePath = $null }

        if ([string]::IsNullOrWhiteSpace($exePath)) { continue }
        if (-not ([string]::Equals($exePath, $ExpectedExePath, [System.StringComparison]::OrdinalIgnoreCase))) { continue }

        if (-not $KillRunning) {
            Write-Error "$ProcessName is running from target path and will lock publish output:`n  $exePath`nClose it or re-run with -KillRunning."
            exit 1
        }

        Write-Host "Stopping $ProcessName PID $($p.Id) from $exePath"
        Stop-Process -Id $p.Id -Force -ErrorAction Stop
    }
}

$projectPath = Resolve-FullPath $Project
if (-not (Test-Path -LiteralPath $projectPath)) {
    Write-Error "Project not found: $projectPath"
    exit 1
}

$repoRoot = Resolve-FullPath (Split-Path -Parent $projectPath)
if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Join-Path $repoRoot ("publish-" + $Configuration.ToLowerInvariant())
}

if (-not (Test-Path -LiteralPath $Target)) {
    New-Item -ItemType Directory -Path $Target -Force | Out-Null
}

$exeName = Get-ProjectExeName $projectPath
$targetExe = Join-Path $Target ($exeName + ".exe")

Stop-AppIfRunning -ExpectedExePath $targetExe -ProcessName $exeName

$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "-o", $OutDir,
    "-v", "minimal"
)

if ($SelfContained) {
    $publishArgs += "--self-contained"
    $publishArgs += "true"
}

foreach ($p in $MsbuildProps) {
    if (-not [string]::IsNullOrWhiteSpace($p)) {
        $publishArgs += $p
    }
}

foreach ($a in $AdditionalDotnetArgs) {
    if (-not [string]::IsNullOrWhiteSpace($a)) {
        $publishArgs += $a
    }
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

robocopy $OutDir $Target /E /MIR /NFL /NDL /NJH /NJS | Out-Null
$robocode = $LASTEXITCODE
if ($robocode -ge 8) {
    Write-Error "robocopy failed with exit code $robocode"
    exit $robocode
}

if (-not (Test-Path -LiteralPath $targetExe)) {
    Write-Error "Expected exe not found: $targetExe"
    exit 1
}

$publishExe = Join-Path $OutDir ($exeName + ".exe")
$publishTs = (Test-Path -LiteralPath $publishExe) ? (Get-Item -LiteralPath $publishExe).LastWriteTimeUtc : $null
$targetTs = (Get-Item -LiteralPath $targetExe).LastWriteTimeUtc

$ts = $targetTs.ToString("o")
Write-Host ""
Write-Host "OK: $targetExe  (UTC $ts)"
if ($publishTs) { Write-Host "     publish: $($publishTs.ToString('o'))" }
Write-Host "     target:  $($targetTs.ToString('o'))"
Write-Host ""

