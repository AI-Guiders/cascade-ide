# GlassCore · shared carve plan (v0)

North star: Avalonia CIDE + WPF glass both **ProjectReference** `CascadeIDE.GlassCore`. UI = projector only.

## Peel1–3 (done)

- Settings merge + presentation builders + `CockpitPresentationLayoutPolicy`.
- Cds/HostSurface/Shell compositors + Display* POCOs.
- DataBus contract/events + `DebugSessionSnapshot` (loader stays host).

## Peel4 (this slice)

- Graph domain model: `GraphDocument` / nodes / edges / kind / JSON / blueprint.
- Deferred: `IGraphDataSource` + `GraphNavigationJsonRequest` (CodeNavigation settings), layout engines (`Avalonia.Point`), PrimitivesKit, `UiLayoutSnapshot`.

## Next peels

1. Abstract `Avalonia.Point` → shared geometry; then move layout engines.
2. Optional: graph source request DTOs without full `CodeNavigationSettings`.
3. Defer: `PrimitivesKit`, `UiLayoutSnapshot`, Avalonia Views/VMs.
4. Optional: grow peel toward full `SettingsService` without OutWit in GlassCore.

Map: inventory `glass-core-settings-inventory-v0.md`; reuse note `glass-wpf-reuse-cide-settings-v0.md`.
