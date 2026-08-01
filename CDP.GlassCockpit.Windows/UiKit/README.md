# WPF Glass UiKit

Adapt of Avalonia `Views/UiKit` into electric Glass — not a blind AXAML port.

| Control | Avalonia source | Role |
|---------|-----------------|------|
| `GlassStatusChip` | `CascadeStatusChip` | Indication chips (Quiet/Caution/Warn/Fail) |
| `GlassEcamReadout` | `EcamReadout` | Label / value / sub instrument tile |
| `GlassSection` | `CascadeSection` | Flat inset + electric rail (`ContentControl`) |

Tokens live in `../GlassDarkCockpit.xaml` (merged from `App.xaml`). SoftOrgan band uses `GlassStatusChip` + `GlassChipLevel` (GlassCore).
