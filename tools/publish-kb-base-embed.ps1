#!/usr/bin/env pwsh
# Копирует собранный kb-base-cide.zip в KbBase/ (резервный путь: publish встроен в agent-notes scripts/build-kb-base-cide.ps1).
# Используй, если сборка была с -SkipPublishToCascadeIde или zip лежит не в dist по умолчанию.
# Пример из корня cascade-ide:  pwsh ./tools/publish-kb-base-embed.ps1
# Пример с явными путями:       pwsh ./tools/publish-kb-base-embed.ps1 -SourceZip D:\repos\agent-notes\dist\kb-base-cide.zip

param(
    [Parameter(HelpMessage = "Прямой путь к kb-base-cide.zip")]
    [string] $SourceZip = "",

    [Parameter(HelpMessage = "Корень репозитория agent-notes (ожидается dist/kb-base-cide.zip)")]
    [string] $AgentNotesRoot = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$cascadeIdeRoot = Split-Path -Parent $PSScriptRoot
$targetDir = Join-Path $cascadeIdeRoot "KbBase"
$targetZip = Join-Path $targetDir "kb-base-cide.zip"

$openRoot = Split-Path -Parent $cascadeIdeRoot
$defaultAgentNotesDist = Join-Path $openRoot "agent-notes/dist/kb-base-cide.zip"

$resolvedSource = ""
if ($SourceZip -ne "") {
    $resolvedSource = (Resolve-Path -LiteralPath $SourceZip).Path
}
elseif ($AgentNotesRoot -ne "") {
    $candidate = Join-Path $AgentNotesRoot "dist/kb-base-cide.zip"
    $resolvedSource = (Resolve-Path -LiteralPath $candidate).Path
}
elseif (Test-Path -LiteralPath $defaultAgentNotesDist) {
    $resolvedSource = (Resolve-Path -LiteralPath $defaultAgentNotesDist).Path
}

if ([string]::IsNullOrWhiteSpace($resolvedSource) -or -not (Test-Path -LiteralPath $resolvedSource)) {
    Write-Error @"
Не найден источник zip. Укажи один из вариантов:
  -SourceZip <полный путь к kb-base-cide.zip>
  -AgentNotesRoot <корень agent-notes>
Или положи сборку по пути sibling: $defaultAgentNotesDist
"@
    exit 2
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Copy-Item -LiteralPath $resolvedSource -Destination $targetZip -Force
Write-Host "OK -> $targetZip"
Write-Host "   <- $resolvedSource"
