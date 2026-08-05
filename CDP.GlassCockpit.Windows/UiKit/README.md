# WPF Glass UiKit

Adapt of Avalonia `Views/UiKit` into electric Glass — **not** a blind AXAML/ECAM port.
One edit-locus for the modern WPF cabin rhythm: tokens in `../GlassDarkCockpit.xaml`, controls here.

| Control | Avalonia cousin | Role |
|---------|-----------------|------|
| `GlassStatusChip` | `CascadeStatusChip` | Indication chips (Quiet/Caution/Warn/Fail) |
| `GlassEcamReadout` | `EcamReadout` | Label / value / sub instrument tile |
| `GlassSection` | `CascadeSection` | Flat inset + electric rail (`ContentControl`) |
| `GlassSoftKeyBar` | `EcamSoftKeyBar` | Action keys (search/reindex/…) — SoftKey tokens, not ECAM green |
| `GlassDeckCard` | `EcamMetricCard` (spirit) | Tone deck card (`FromChip`) — SoftOrgan/HCI instruments |

SoftOrgan band uses `GlassStatusChip` + `GlassChipLevel` (GlassCore).
Deck surfaces: `CreateDeckCard` → `GlassDeckCard.FromChip`.
HybridIndex hand: `GlassSoftKeyBar` + search box.
