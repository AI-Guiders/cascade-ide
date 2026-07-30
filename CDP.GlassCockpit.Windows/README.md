# CDP.GlassCockpit.Windows

WPF operator glass for Cognitive Dev Platform (ADR-0021).
Separate process · latch IPC · Avalonia on hold for Windows primary.

## Stack (peel0)

- **AvalonDock** — P | F | M layout
- **AvalonEdit** — Forward = Intercom long-form (**body** painted; not raw latch JSON)
- **LatchHub + LatchPaint** — `%LocalAppData%/cdp-mcp/*-LATEST.json` → human glass

North star: full-parity with Cascade IDE glass; Monaco/WebView2, MFD pages, Semantic Map — later peels.

## Run

```powershell
cd CDP.GlassCockpit.Windows
dotnet run -c Release
```

Exe (after build):

`bin/Release/net10.0-windows/CDP.GlassCockpit.Windows.exe`

Point CDP Start/Stop at it:

```toml
# cdp-mcp.toml
[cockpit_host]
exe = "D:/…/CDP.GlassCockpit.Windows/bin/Release/net10.0-windows/CDP.GlassCockpit.Windows.exe"
```

or `cdp_cockpit_host op=start path=…`

## Linux

Not this project. Later: native glass (Qt/…) or Avalonia as `GlassCockpit.Linux` — habitat stays platform-agnostic.
