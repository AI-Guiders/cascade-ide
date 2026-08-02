# Dogfood highlight + appearance against live Glass.
$ErrorActionPreference = 'Stop'
$root = Join-Path $env:LOCALAPPDATA 'cdp-mcp'
function Invoke-SurfaceOp([string]$op, [hashtable]$argsMap) {
    $id = [guid]::NewGuid().ToString('N')
    $cmdPath = Join-Path $root 'surface-cmd-LATEST.json'
    $replyPath = Join-Path $root 'surface-reply-LATEST.json'
    $body = @{
        schema      = 'agent_surface/v0'
        id          = $id
        op          = $op
        stamped_utc = (Get-Date).ToUniversalTime().ToString('o')
    }
    if ($argsMap -and $argsMap.Count -gt 0) { $body.args = $argsMap }
    Remove-Item $replyPath -ErrorAction SilentlyContinue
    Set-Content -Path $cmdPath -Value ($body | ConvertTo-Json -Compress -Depth 6) -Encoding utf8
    $deadline = (Get-Date).AddSeconds(8)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $replyPath) {
            $r = Get-Content $replyPath -Raw | ConvertFrom-Json
            if ($r.id -eq $id) {
                Write-Output ("{0}: ok={1} detail={2}" -f $op, $r.ok, $(if ($r.detail) { $r.detail } else { $r.result }))
                return $r
            }
        }
        Start-Sleep -Milliseconds 40
    }
    throw "TIMEOUT $op"
}
Invoke-SurfaceOp 'highlight' @{ name = 'SendBtn' }
Invoke-SurfaceOp 'appearance' @{ name = 'SendBtn' }
Invoke-SurfaceOp 'focus' @{ name = 'ComposerBox' }
Write-Output 'dogfood aim/drive smoke OK'
