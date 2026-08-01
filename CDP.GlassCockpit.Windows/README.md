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
- **LatchHub + LatchPaint** — latch → human glass (not raw JSON dump); `LatchHub` (~100 LOC; under gate — no peel) watches `*-LATEST.json`; SoftOrgan chrome_hint in `LatchPaint.SoftOrgan`, EICAS in `LatchPaint.Eicas`
- **SoftOrgan chrome band** — `*-LATEST.json` chrome_hint → top-N VisibleLines + Overflow chip (click expand/collapse; GlassCore density; `MainWindow.SoftOrganBand` paint; includes `sa-desk`)

## MainWindow partials (0-sync)

`MainWindow.xaml.cs` is a thin ctor shell. Surface peels:

| Partial | Owns |
| --- | --- |
| `MainWindow.LayoutSurface.cs` | session layout, host sync, Forward `primary_work_surface` (ADR 0120) |
| `MainWindow.EditorSurface.cs` | AvalonEdit mount, dogfood open, pick/save, Ctrl+O/S |
| `MainWindow.IntercomFeed.cs` | latch watch, journal append, composer send (~114 LOC) |
| `MainWindow.IntercomFeed.Rebuild.cs` | feed rebuild, topics, scroll pin, new-msg cue (~123 LOC) |
| `MainWindow.SoftOrganBand.cs` | SoftOrgan latch → band paint / overflow toggle (~50 LOC; under gate — no peel) |
| `MainWindow.LatchEicas.cs` | presentation / alert / qrh latch → Plan + EICAS (~93 LOC; under gate — no peel); uses `EicasBandAggregator` (~42 LOC; under gate — no peel) |
| `MainWindow.MfdBody.cs` | MFD page select + stub body text |

`LatchPaint` partials (static paint helpers):

| Partial | Owns |
| --- | --- |
| `LatchPaint.cs` | Intercom + Presentation + shared `Prop` (~125 LOC) |
| `LatchPaint.SoftOrgan.cs` | `TryReadChromeHint` for SoftOrgan band |
| `LatchPaint.Eicas.cs` | alert/qrh → EICAS status lines |

Shared SoftOrgan density: `CascadeIDE.GlassCore/SoftOrgan/SoftOrganChromeDensityPolicy.cs` (Avalonia façade forwards).
Latch id catalog (Glass `*-LATEST.json` stems): `SoftOrganLatchCatalog` — consumed by `LatchHub`.
SSOT triangle map: `CascadeIDE.GlassCore/SoftOrgan/README.md`.
Avalonia SoftOrgan/CabinOrgan ViewModel map: `ViewModels/README.md`.

## Dogfood look (operator checklist)

Glass is a **projector** (latches → pixels), not the agent loop. With CDP live, check:

1. **SoftOrgan strip (top)** — `UiKit/GlassStatusChip` (Quiet/Caution/Warn/Fail); full chrome_hint in Tip only. `+N` / `−` expand. CFG chip = settings tip. Electric rail on indication band.
2. **sa-desk** — appears in SoftOrgan catalog (quiet-chrome); should participate in band ordering, not as EICAS id.
3. **EICAS / Plan** — alert/qrh still via `LatchPaint.Eicas` + `MainWindow.LatchEicas` (not SoftOrgan catalog members).
4. **Intercom feed** — journal/composer still live via Intercom latches.
5. **Forward / editor** — AvalonEdit mount + layout sync (LayoutSurface / EditorSurface).
6. **MFD glance** — one page fills M (title `M · {page}`); catalog hidden — switch via seats/palette, not mouse list. Build/Terminal/… hosts; peers via SoftOrgan glance/stub.
7. **PFD** — one Plan glance (no subtitle stack / nav list).
8. **AE** — Cascadia 14.5 · TextMateSharp Dark+ (`GlassAvalonEditTextMate`, WPF port of AvaloniaEdit.TextMate) · XSHD fallback · folding/search/current-line (`GlassAvalonEditChrome`).
9. **Crash note** — Cursor OOM / «Window terminated» on Avalonia CIDE ≠ Glass failure; Glass process name `CDP.GlassCockpit.Windows`.

Point `[cockpit_host] exe` at this Glass exe if Start/Stop should launch WPF (not `publish/CascadeIDE.exe`).

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
