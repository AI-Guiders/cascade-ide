# CDP.GlassCockpit.Windows

WPF operator glass for Cognitive Dev Platform (ADR-0021).
Separate process · latch IPC · Avalonia on hold for Windows primary.

## Reuse (not reinvent)

- **Settings/topology:** `%LocalAppData%/CascadeIDE/settings.toml` via `CascadeIDE.GlassCore`
- **Layout math:** same `Services/Presentation/*` builders as Avalonia CIDE (linked into GlassCore)
- **Live patch:** `%LocalAppData%/cdp-mcp/presentation-LATEST.json` + intercom latch
- **UI only:** WPF columns/views — see `docs/design/glass-wpf-reuse-cide-settings-v0.md`

## Stack

- **GlassCore** — presentation parser/topology + settings peel
- **WPF MainGrid** — `WpfMainGridColumns` (no Avalonia ColumnDefinitions.Parse)
- **LatchHub + LatchPaint** — latch → human glass (not raw JSON dump)
- **SoftOrgan chrome band** — `*-LATEST.json` chrome_hint → top SoftOrganHint (density collapse, parity Avalonia)

## Run

```powershell
cd CDP.GlassCockpit.Windows
dotnet run -c Release
```

Exe: `bin/Release/net10.0-windows/CDP.GlassCockpit.Windows.exe`

Point CDP Start/Stop:

```toml
[cockpit_host]
exe = "D:/…/CDP.GlassCockpit.Windows/bin/Release/net10.0-windows/CDP.GlassCockpit.Windows.exe"
```
