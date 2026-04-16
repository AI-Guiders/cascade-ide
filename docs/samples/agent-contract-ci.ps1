# Пример: проверка контракта агента (--agent-contract) из CI на PowerShell.
# В репозитории чаще используют dotnet-script — см. agent-contract-ci.csx рядом.
# Рекомендуется PowerShell 7+ (pwsh): после вызова нативного .exe доступен $LASTEXITCODE.
# В Windows PowerShell 5.1 $LASTEXITCODE после & exe не всегда заполняется — используй Start-Process -PassThru.ExitCode или pwsh.

param(
    [Parameter(Mandatory = $true, HelpMessage = "Путь к CascadeIDE.exe после publish/build.")]
    [string] $CascadeIdeExe,
    [string] $Workspace = (Get-Location).ProviderPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $CascadeIdeExe)) {
    throw "CascadeIDE not found: $CascadeIdeExe"
}

function Invoke-AgentContract {
    param([string[]] $Arguments)
    $raw = & $CascadeIdeExe @Arguments 2>&1
    $code = $LASTEXITCODE
    $text = if ($null -eq $raw) {
        ""
    } elseif ($raw -is [System.Array]) {
        $raw -join [Environment]::NewLine
    } else {
        [string]$raw
    }
    if ($code -ne 0) {
        throw "agent-contract failed (exit $code): $text"
    }
    return $text
}

# Без git — только stdout JSON
$uiModesJson = Invoke-AgentContract -Arguments @("--agent-contract", "get_ui_modes_diagnostics")
if ([string]::IsNullOrWhiteSpace($uiModesJson)) { throw "empty JSON from get_ui_modes_diagnostics" }

$langJson = Invoke-AgentContract -Arguments @("--agent-contract", "get_supported_editor_languages")

# Git — явный корень репозитория (как в MCP)
$gitJson = Invoke-AgentContract -Arguments @("--agent-contract", "--workspace", $Workspace, "git_status")

Write-Host "OK: ui_modes length=$($uiModesJson.Length), languages length=$($langJson.Length), git_status length=$($gitJson.Length)"
