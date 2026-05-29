# Регистрация cide:// для dev (ADR 0157). Запуск от текущего пользователя (HKCU).
param(
    [string]$ExePath = "",
    [switch]$Unregister
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ExePath)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $candidates = @(
        (Join-Path $repoRoot "bin\Debug\net10.0\win-x64\CascadeIDE.exe"),
        (Join-Path $repoRoot "bin\Debug\net10.0\CascadeIDE.exe"),
        "D:\cascade-ide-debug\CascadeIDE.exe"
    )
    foreach ($c in $candidates) {
        if (Test-Path -LiteralPath $c) {
            $ExePath = (Resolve-Path -LiteralPath $c).Path
            break
        }
    }
}

if ([string]::IsNullOrWhiteSpace($ExePath) -or -not (Test-Path -LiteralPath $ExePath)) {
    Write-Error "CascadeIDE.exe not found. Pass -ExePath or publish debug first."
}

$scheme = "cide"
$baseKey = "Registry::HKEY_CURRENT_USER\Software\Classes\$scheme"

if ($Unregister) {
    if (Test-Path -LiteralPath $baseKey) {
        Remove-Item -LiteralPath $baseKey -Recurse -Force
        Write-Host "Removed $scheme:// protocol registration."
    }
    return
}

New-Item -Path $baseKey -Force | Out-Null
Set-ItemProperty -Path $baseKey -Name "(Default)" -Value "URL:Cascade IDE Magic Link"
Set-ItemProperty -Path $baseKey -Name "URL Protocol" -Value ""

$iconKey = Join-Path $baseKey "DefaultIcon"
New-Item -Path $iconKey -Force | Out-Null
Set-ItemProperty -Path $iconKey -Name "(Default)" -Value "$ExePath,0"

$commandKey = Join-Path $baseKey "shell\open\command"
New-Item -Path $commandKey -Force | Out-Null
$command = "`"$ExePath`" `"%1`""
Set-ItemProperty -Path $commandKey -Name "(Default)" -Value $command

Write-Host "Registered $scheme:// -> $ExePath"
Write-Host "Example: ${scheme}://reveal?root=$( [uri]::EscapeDataString($repoRoot) )&f=Program.cs&l=1"
