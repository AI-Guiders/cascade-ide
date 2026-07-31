# SoftOrgan quiet chrome (GlassCore SSOT)

Shared SoftOrgan latch → density → band math for **Glass WPF** and **Avalonia CIDE**.
Hosts own UI paint only; ids and collapse policy live here.

## Triangle

```
  SoftOrganLatchCatalog          SoftOrganChromeDensityPolicy
  (who is a SoftOrgan)     ←→    (priority + Collapse/From)
              \                      /
               \                    /
                SoftOrganChromeAggregator
                (Glass hint store + Snapshot band)
```

| Type | Owns |
| --- | --- |
| `SoftOrganLatchCatalog` | Canonical latch stems (`{id}-LATEST.json`); `SaDesk` / `Canonicalize` (`sa_desk` → `sa-desk`); `Contains` / `TryParseFileName` |
| `SoftOrganChromeDensityPolicy` | Priority table, `From`/`Collapse`/`ToggleExpanded`; `From`/`PriorityFor` go through `Canonicalize` |
| `SoftOrganChromeAggregator` | Glass in-memory hints; `Apply` gated by catalog; Snapshot → VisibleLines + overflow |

EICAS ids (`alert` / `qrh` / …) stay on the density priority table for ordering when mixed, but are **not** SoftOrgan latch catalog members — Aggregator ignores them.

## Host peels

| Host | Path |
| --- | --- |
| Glass latch I/O | `CDP.GlassCockpit.Windows/LatchHub` → catalog parse; `LatchPaint.SoftOrgan` → `TryReadChromeHint` |
| Glass band UI | `MainWindow.SoftOrganBand` (~50 LOC; under gate) |
| Avalonia VM | `ViewModels/MainWindowViewModel.SoftOrganChrome` + `CabinOrganChrome`; façade `Features/UiChrome/AgentChromeHintDensityPolicy` |

Maps: [Glass README](../../CDP.GlassCockpit.Windows/README.md) · [ViewModels README](../../ViewModels/README.md).

## New SoftOrgan seat checklist

1. Add id to `SoftOrganLatchCatalog.Ids` (latch stem = file prefix).
2. Add priority arm in `SoftOrganChromeDensityPolicy.PriorityFor` (after `Canonicalize`).
3. Glass: LatchHub already iterates catalog — drop `{id}-LATEST.json` with `chrome_hint`.
4. Avalonia: CabinOrgan field + SoftOrgan seats table row using catalog id (`SaDesk`, not `sa_desk`) + Show/Apply one-liners.
