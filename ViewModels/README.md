# Avalonia MainWindowViewModel — SoftInstrument chrome partials

Context-economy peels for quiet SoftInstrument chrome (parity with Glass `MainWindow.SoftInstrumentBand`).
SSOT triangle (Catalog · Density · Aggregator): `CascadeIDE.GlassCore/SoftInstrument/README.md`.
Density SSOT: `CascadeIDE.GlassCore/SoftInstrument/SoftInstrumentChromeDensityPolicy.cs`.
Latch id catalog: `SoftInstrumentLatchCatalog` (Avalonia seat ids must match — use `SaDesk` / `sa-desk`, not `sa_desk`).
Avalonia façade: `Features/UiChrome/AgentChromeHintDensityPolicy.cs`.

## SoftInstrument / CabinOrgan (0-sync)

| Partial | Owns |
| --- | --- |
| `MainWindowViewModel.CabinOrganChrome.cs` | `Agent*ChromeHint` ObservableProperty fields + `NotifyPropertyChangedFor` (Show*, WorkspaceBand, density props) |
| `MainWindowViewModel.SoftInstrumentChrome.cs` | Show* flags, `ShowWorkspaceChromeBand`, `Apply*ChromeHint` + normalize helper, VisibleLines/Overflow/Toggle, seat candidate table |
| `MainWindowViewModel.ShellConstruction.cs` | Ctor shell: children VM, settings bootstrap, IdeMcp/bus/agent, LSP/DAP |
| `MainWindowViewModel.ShellConstruction.Panels.cs` | Panel factory + post-construct wire (Chat/Git/Build/…) |
| `MainWindowViewModel.ShellConstruction.HealthPresentation.cs` | Health/EICAS/presentation factory + post-construct wire |
| `MainWindowViewModel.ShellConstruction.GlassPatch.cs` | Live topology/tier/instruments glass patch → settings.toml |
| `MainWindowViewModel.ShellConstruction.Diagnose.cs` | Diagnose-files / warmup path helpers for agent environment |
| `MainWindowViewModel.PfdBackgroundStatus.cs` | PFD/Forward status strip props + warmup/HCI refresh (~152 LOC) |
| `MainWindowViewModel.PfdBackgroundStatus.VerifyEpoch.cs` | Verify Epoch apply, agent cancel/retry, ticker, hide timers |
| `MainWindowViewModel.SettingsReactive.cs` | Markdown/MCP/AI mode/keys/chat chord reactions |
| `MainWindowViewModel.SettingsReactive.HybridIndex.cs` | Workspace splitters + Hybrid Index (HCI) reactions |
| `MainWindowViewModel.SettingsReactive.Intercom.cs` | Intercom transport field reactions |
| `MainWindowViewModel.ShellSession.cs` | ShellChrome proxies + property-changed relay (~119 LOC) |
| `MainWindowViewModel.ShellSession.Handlers.cs` | HandleShell* UI mode / panels / MFD page |
| `MainWindowViewModel.Presentation.cs` | Host surface layout, editor groups (~73 LOC; under gate — peels already extracted) |
| `MainWindowViewModel.Presentation.Capabilities.cs` | UiMode Capabilities + instrumentation dock flags (~27 LOC; under gate) |
| `MainWindowViewModel.Presentation.Skia.cs` | Zone-geometry overlay + instrument mount styles (~42 LOC; under gate) |
| `MainWindowViewModel.Presentation.IdeHealth.cs` | IDE Health strip/EICAS/bottom chrome, cockpit shorts, Skia mount contexts (~104 LOC; under gate) |
| `MainWindowViewModel.Presentation.Badges.cs` | Safety level + risk/result/LOC/progress badges (~42 LOC; under gate) |
| `MainWindowViewModel.Presentation.Regions.cs` | Region collapse, panel-hidden, MFD contour + MfdRegion aliases (~30 LOC; under gate) |

Call sites keep public `Apply*ChromeHint` names. New chrome seats: add field (+ Notify attrs) in CabinOrgan, then one row in SoftInstrument seats table + Show/Apply one-liners.

Glass map: `CDP.GlassCockpit.Windows/README.md` → MainWindow partials.
