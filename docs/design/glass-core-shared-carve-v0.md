# GlassCore · shared carve plan (v0)

North star: Avalonia CIDE + WPF glass both **ProjectReference** `CascadeIDE.GlassCore`. UI = projector only.

## Peel1 (done)

- Settings merge peel: `defaults-settings.toml` → `.cascade/workspace.toml` → user `settings.toml` (`IdeGlassSettings` + `GlassTomlMerge`).
- Expand linked presentation builders (compact/ultrawide/split + `PresentationMonitorSnapshot`).
- First Cockpit aviation: `CockpitPresentationLayoutPolicy` + `PresentationTierKind`.
- CascadeIDE **ProjectReference** GlassCore; Compile Remove of linked sources (Avalonia monitor probes stay in CascadeIDE).

## Peel2 (done)

- `Cds/`: `ICdsRouter`, `CockpitSurfaceState`, `AttentionLayoutSurfaceKind` (snapshot builder stays host — ViewModels).
- HostSurface IDs/placement/compositors + Shell compositor.
- Display* POCOs + routing keys/mount policy ids; `AgentSafetyLevel`.

## Peel3 (this slice)

- `IDataBus` + `InMemoryDataBus` + `DataBusEventPolicy` (+ `AllReliable` default).
- Channel event DTOs + `DebugSessionSnapshot` (Avalonia-free).
- `DataBusEventPolicyLoader` stays in CascadeIDE (embedded TOML / `BundledAppContent`); host constructs bus with `Loader.Load()`.

## Next peels

1. ~~`Cds/` + HostSurface IDs/placement/compositors (Avalonia-free).~~
2. ~~`DataBus` + channel contracts; paint stays in host.~~
3. Graph model before layout engines; abstract `Avalonia.Point`.
4. Defer: `PrimitivesKit`, `UiLayoutSnapshot`, Avalonia Views/VMs.
5. Optional: grow peel toward full `SettingsService` without OutWit in GlassCore.

Map: inventory `glass-core-settings-inventory-v0.md`; reuse note `glass-wpf-reuse-cide-settings-v0.md`.
