# SoftInstrument quiet chrome (GlassCore SSOT)

Shared SoftInstrument latch → density → band math for **Glass WPF** and **Avalonia CIDE**.
Hosts own UI paint only; ids and collapse policy live here.

## Triangle

```
  SoftInstrumentLatchCatalog          SoftInstrumentChromeDensityPolicy
  (who is a SoftInstrument)     ←→    (priority + Collapse/From)
              \                      /
               \                    /
                SoftInstrumentChromeAggregator
                (Glass hint store + Snapshot band)
```

| Type | Owns |
| --- | --- |
| `SoftInstrumentLatchCatalog` | Canonical latch stems (`{id}-LATEST.json`); `SaDesk` / `Canonicalize` (`sa_desk` → `sa-desk`); `Contains` / `TryParseFileName` |
| `SoftInstrumentChromeDensityPolicy` | Priority table, `From`/`Collapse`/`ToggleExpanded`; `From`/`PriorityFor` go through `Canonicalize` |
| `SoftInstrumentChromeAggregator` | Glass in-memory hints; `Apply` gated by catalog; Snapshot → VisibleLines + overflow |
| `SoftInstrumentMfdGlance` | MFD page → SoftInstrument latch glance body (`Build`←toolchain, `Terminal`←sys) |

EICAS ids (`alert` / `qrh` / …) stay on the density priority table for ordering when mixed, but are **not** SoftInstrument latch catalog members — Aggregator ignores them.

## Host peels

| Host | Path |
| --- | --- |
| Glass latch I/O | `CDP.GlassCockpit.Windows/LatchHub` → catalog parse; `LatchPaint.SoftInstrument` → `TryReadChromeHint` |
| Glass band UI | `MainWindow.SoftInstrumentBand` (~50 LOC; under gate) |
| Avalonia VM | `ViewModels/MainWindowViewModel.SoftInstrumentChrome` + `CabinOrganChrome`; façade `Features/UiChrome/AgentChromeHintDensityPolicy` |

Maps: [Glass README](../../CDP.GlassCockpit.Windows/README.md) · [ViewModels README](../../ViewModels/README.md).

## New SoftInstrument seat checklist

1. Add id to `SoftInstrumentLatchCatalog.Ids` (latch stem = file prefix).
2. Add priority arm in `SoftInstrumentChromeDensityPolicy.PriorityFor` (after `Canonicalize`).
3. Glass: LatchHub already iterates catalog — drop `{id}-LATEST.json` with `chrome_hint`.
4. Avalonia: CabinOrgan field + SoftInstrument seats table row using catalog id (`SaDesk`, not `sa_desk`) + Show/Apply one-liners.
