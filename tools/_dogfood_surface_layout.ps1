# Dogfood agent_surface layout RPC against live Glass.
$ErrorActionPreference = 'Stop'
$root = Join-Path $env:LOCALAPPDATA 'cdp-mcp'
$id = [guid]::NewGuid().ToString('N')
$cmdPath = Join-Path $root 'surface-cmd-LATEST.json'
$replyPath = Join-Path $root 'surface-reply-LATEST.json'
$cmd = @{
    schema      = 'agent_surface/v0'
    id          = $id
    op          = 'layout'
    stamped_utc = (Get-Date).ToUniversalTime().ToString('o')
} | ConvertTo-Json -Compress
Remove-Item $replyPath -ErrorAction SilentlyContinue
Set-Content -Path $cmdPath -Value $cmd -Encoding utf8
$deadline = (Get-Date).AddSeconds(8)
while ((Get-Date) -lt $deadline) {
    if (Test-Path $replyPath) {
        $r = Get-Content $replyPath -Raw | ConvertFrom-Json
        if ($r.id -eq $id) {
            $roles = @($r.result.windows | ForEach-Object { $_.role }) -join ','
            Write-Output "ok=$($r.ok) windows=$($r.result.windows.Count) roles=$roles"
            $names = [System.Collections.Generic.List[string]]::new()
            function Walk([object]$n) {
                if ($null -eq $n) { return }
                if ($n.name) { [void]$names.Add([string]$n.name) }
                if ($n.children) {
                    foreach ($c in @($n.children)) { Walk $c }
                }
            }
            foreach ($w in @($r.result.windows)) { Walk $w.root }
            Write-Output ('named_sample=' + (($names | Select-Object -First 16) -join ','))
            exit 0
        }
    }
    Start-Sleep -Milliseconds 50
}
Write-Output 'TIMEOUT'
Get-Process -Name CDP.GlassCockpit.Windows -ErrorAction SilentlyContinue | Format-Table Id, ProcessName
exit 1
