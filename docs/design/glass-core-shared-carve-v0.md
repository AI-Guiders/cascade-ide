# GlassCore · shared carve plan (v0)

North star: Avalonia CIDE + WPF glass both **ProjectReference** `CascadeIDE.GlassCore`. UI = projector only.

## Peel1–4 (done)

- Settings + presentation + Cds/HostSurface/DataBus + Graph domain model.

## Peel5 (done)

- `CascadeIDE.Primitives.Point2D` in GlassCore.
- Graph layout scene/engines + binding VM layouts use `Point2D` (no Avalonia).
- Skia paint boundary: `GraphPointAvalonia.ToAv()`.

## Peel5b (done)

- Graph `Layout/` engines + presentation/metrics linked into GlassCore.
- Thin Models: DetailLevel / RelatedGraphLayoutKind / LevelKind / ControlFlowMainAxisKind.
- `LevelKind.Normalize` owns depth; `Settings.NormalizeDepth` delegates (no Settings pull into GlassCore).

## Next peels

1. Defer: `PrimitivesKit`, `UiLayoutSnapshot`, Avalonia Views/VMs.
2. Optional: grow peel toward full `SettingsService` without OutWit in GlassCore.
3. Keep host: `DataBusEventPolicyLoader`, `IGraphDataSource` / navigation JSON request.

Map: inventory `glass-core-settings-inventory-v0.md`; reuse note `glass-wpf-reuse-cide-settings-v0.md`.
