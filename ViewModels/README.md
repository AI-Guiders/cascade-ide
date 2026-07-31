# Avalonia MainWindowViewModel — SoftOrgan chrome partials

Context-economy peels for quiet SoftOrgan chrome (parity with Glass `MainWindow.SoftOrganBand`).
Density SSOT: `CascadeIDE.GlassCore/SoftOrgan/SoftOrganChromeDensityPolicy.cs`.
Avalonia façade: `Features/UiChrome/AgentChromeHintDensityPolicy.cs`.

## SoftOrgan / CabinOrgan (0-sync)

| Partial | Owns |
| --- | --- |
| `MainWindowViewModel.CabinOrganChrome.cs` | `Agent*ChromeHint` ObservableProperty fields + `NotifyPropertyChangedFor` (Show*, WorkspaceBand, density props) |
| `MainWindowViewModel.SoftOrganChrome.cs` | Show* flags, `ShowWorkspaceChromeBand`, `Apply*ChromeHint` + normalize helper, VisibleLines/Overflow/Toggle, seat candidate table |

Call sites keep public `Apply*ChromeHint` names. New chrome seats: add field (+ Notify attrs) in CabinOrgan, then one row in SoftOrgan seats table + Show/Apply one-liners.

Glass map: `CDP.GlassCockpit.Windows/README.md` → MainWindow partials.
