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

## Peel6 (done)

- `UserSettingsPaths` linked into GlassCore; `IdeGlassSettings.DefaultSettingsPath` delegates to it.
- GlassCore ProjectReference `CascadeIDE.Contracts` (for `[IoBoundary]`).
- Full `SettingsService` / `CascadeIdeSettings` / OutWit stay host.

## Peel6b (done)

- `UserSettingsTomlFileAccess` + `TextFileReadWrite` linked into GlassCore.
- `IdeGlassSettings` user-toml read goes through `TextFileReadWrite` (safe I/O).

## Peel6c (done)

- `WorkspaceCascadePaths` (path + cwd discovery) shared via GlassCore.
- `IdeGlassSettings` + `RepositoryWorkspaceTomlLoader` use it; typed `RepositoryWorkspaceToml` stays host.

## Peel7 (done)

- `SettingsDefaultsPaths` — discovery/read for `defaults-settings.toml` (disk / walk-up / embedded).
- `IdeGlassSettings` delegates; `SettingsDefaultsLoader.BundledRelativePath` aliases the shared constant.
- Host still owns `BundledAppContent` + typed `CascadeIdeSettings` merge.

## Peel8 (done)

- `CdpHabitatPaths` — `%LocalAppData%/cdp-mcp` state root + latch file helpers (`presentation-LATEST`, `intercom-LATEST`, `GetLatchPath`).
- Avalonia CDP projectors + WPF `LatchHub` share StateRoot; watchers/JSON apply stay host.

## Peel9 (done)

- `CdpLatchIo` — toolkit-agnostic settle (`PostSettled` / `PostSettledIfExists`) + `TryReadAllTextIfExists`.
- Host `CdpLatchFs.PostApply` = settle then Avalonia UI marshal; WPF `LatchHub` uses `PostSettledIfExists`.

## Peel10 (done)

- Remaining CDP projectors: `File.Exists` + `File.ReadAllText(LatchPath)` → `CdpLatchIo.TryReadAllTextIfExists` (missing → null; chrome callers still Apply*(null)).
- Presentation already on peel9; SharedFile / CRM / Webcam / DiskSync / Land + Alert-style try/catch patterns covered.

## Peel11 (done)

- `AttachmentAnchorPaths` focus-LATEST read → `CdpLatchIo.TryReadAllTextIfExists` (last `File.ReadAllText` on latch paths outside settle helpers).
- WPF `LatchHub.TryFireExisting` keeps `File.Exists` (existence gate only; no content read).

## Peel12 (done)

- Host `Compile Remove` for peel7–9 sources (`SettingsDefaultsPaths`, `CdpHabitatPaths`, `CdpLatchIo`) — single compile via GlassCore (was dual-compile).
- `CanonicalFilePath` linked into GlassCore; host Remove.
- `IdeGlassSettings` user.toml read via `TextFileReadWrite` only (no `File.Exists` gate).

## Next peels

1. Defer: `PrimitivesKit`, `UiLayoutSnapshot`, Avalonia Views/VMs.
2. Keep host: `DataBusEventPolicyLoader`, `IGraphDataSource` / navigation JSON request, full `SettingsService` + typed workspace overlay (`CascadeIdeSettings` : OutWit `ModelBase`).

Map: inventory `glass-core-settings-inventory-v0.md`; reuse note `glass-wpf-reuse-cide-settings-v0.md`.
