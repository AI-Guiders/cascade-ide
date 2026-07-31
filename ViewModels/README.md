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
| `MainWindowViewModel.Presentation.cs` | Host surface layout, editor groups |
| `MainWindowViewModel.Presentation.Capabilities.cs` | UiMode Capabilities + instrumentation dock flags |
| `MainWindowViewModel.Presentation.Skia.cs` | Zone-geometry overlay + instrument mount styles |
| `MainWindowViewModel.Presentation.IdeHealth.cs` | IDE Health strip/EICAS/bottom chrome, cockpit shorts, Skia mount contexts |
| `MainWindowViewModel.Presentation.Badges.cs` | Safety level + risk/result/LOC/progress badges |
| `MainWindowViewModel.Presentation.Regions.cs` | Region collapse, panel-hidden, MFD contour + MfdRegion aliases |

Call sites keep public `Apply*ChromeHint` names. New chrome seats: add field (+ Notify attrs) in CabinOrgan, then one row in SoftOrgan seats table + Show/Apply one-liners.

Glass map: `CDP.GlassCockpit.Windows/README.md` → MainWindow partials.
