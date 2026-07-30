# GlassCore · shared carve plan (v0)

North star: Avalonia CIDE + WPF glass both **ProjectReference** `CascadeIDE.GlassCore`. UI = projector only.

## Peel1–4 (done)

- Settings + presentation + Cds/HostSurface/DataBus + Graph domain model.

## Peel5 (this slice)

- `CascadeIDE.Primitives.Point2D` in GlassCore.
- Graph layout scene/engines + binding VM layouts use `Point2D` (no Avalonia).
- Skia paint boundary: `GraphPointAvalonia.ToAv()`.
- Layout engines still compile in CascadeIDE host (Models deps); link into GlassCore next.

## Next peels

1. Link Avalonia-free Graph `Layout/` (+ needed Models) into GlassCore.
2. Defer: `PrimitivesKit`, `UiLayoutSnapshot`, Avalonia Views/VMs.
3. Optional: grow peel toward full `SettingsService` without OutWit in GlassCore.

Map: inventory `glass-core-settings-inventory-v0.md`; reuse note `glass-wpf-reuse-cide-settings-v0.md`.
