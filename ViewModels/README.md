# Avalonia MainWindowViewModel — SoftOrgan chrome partials

Context-economy peels for quiet SoftOrgan chrome (parity with Glass `MainWindow.SoftOrganBand`).
SSOT triangle (Catalog · Density · Aggregator): `CascadeIDE.GlassCore/SoftOrgan/README.md`.
Density SSOT: `CascadeIDE.GlassCore/SoftOrgan/SoftOrganChromeDensityPolicy.cs`.
Latch id catalog: `SoftOrganLatchCatalog` (Avalonia seat ids must match — use `SaDesk` / `sa-desk`, not `sa_desk`).
Avalonia façade: `Features/UiChrome/AgentChromeHintDensityPolicy.cs`.

## SoftOrgan / CabinOrgan (0-sync)

| Partial | Owns |
| --- | --- |
| `MainWindowViewModel.CabinOrganChrome.cs` | `Agent*ChromeHint` ObservableProperty fields + `NotifyPropertyChangedFor` (Show*, WorkspaceBand, density props) |
| `MainWindowViewModel.SoftOrganChrome.cs` | Show* flags, `ShowWorkspaceChromeBand`, `Apply*ChromeHint` + normalize helper, VisibleLines/Overflow/Toggle, seat candidate table |
| `MainWindowViewModel.ShellConstruction.cs` | Ctor shell: children VM, settings bootstrap, IdeMcp/bus/agent, LSP/DAP |
| `MainWindowViewModel.ShellConstruction.Panels.cs` | Panel factory + post-construct wire (Chat/Git/Build/…) |
| `MainWindowViewModel.ShellConstruction.HealthPresentation.cs` | Health/EICAS/presentation factory + post-construct wire |
| `MainWindowViewModel.ShellConstruction.GlassPatch.cs` | Live topology/tier/instruments glass patch → settings.toml |
| `MainWindowViewModel.ShellConstruction.Diagnose.cs` | Diagnose-files / warmup path helpers for agent environment |
| `MainWindowViewModel.Presentation.cs` | Host surface layout, editor groups (~73 LOC; under gate — peels already extracted) |
| `MainWindowViewModel.Presentation.Capabilities.cs` | UiMode Capabilities + instrumentation dock flags (~27 LOC; under gate) |
| `MainWindowViewModel.Presentation.Skia.cs` | Zone-geometry overlay + instrument mount styles (~42 LOC; under gate) |
| `MainWindowViewModel.Presentation.IdeHealth.cs` | IDE Health strip/EICAS/bottom chrome, cockpit shorts, Skia mount contexts (~104 LOC; under gate) |
| `MainWindowViewModel.Presentation.Badges.cs` | Safety level + risk/result/LOC/progress badges (~42 LOC; under gate) |
| `MainWindowViewModel.Presentation.Regions.cs` | Region collapse, panel-hidden, MFD contour + MfdRegion aliases (~30 LOC; under gate) |

Call sites keep public `Apply*ChromeHint` names. New chrome seats: add field (+ Notify attrs) in CabinOrgan, then one row in SoftOrgan seats table + Show/Apply one-liners.

Glass map: `CDP.GlassCockpit.Windows/README.md` → MainWindow partials.
