<!-- English translation of adr/0090-launch-profiles-and-debug-startup-configurations.md. Canonical Russian: ../../adr/0090-launch-profiles-and-debug-startup-configurations.md -->

# ADR 0090: Launch profiles and multiple debug startup configurations (VS-style)

**Status:** Accepted · Implemented  
**Date:** 2026-04-23

## Related ADRs

| ADR | Role |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | unified debug layer for human and agent |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | user settings — `settings.toml`, `%LocalAppData%\CascadeIDE\`, secrets separate |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML-first config; holistic settings UI **deferred**; point UI is **canon facade** |
| [0093](0093-mfd-embedded-browser-for-launch-url.md) | embedded launch URL on MFD (extends profiles and launchBrowser) |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | `debug_launch` and related tools |
| [MsBuildDebugTargetResolver.cs](../../../Services/MsBuildDebugTargetResolver.cs) | MSBuild debug target resolver |

## Summary

- **Launch profiles** and multiple debug startup configurations (as in VS).
- Storage, MCP, migration from `startup-project.json`.
- Optional URL on MFD — [0093](0093-mfd-embedded-browser-for-launch-url.md).

---

## Context

Today **one solution** has **at most one** explicit startup project: path to `.csproj` in `.cascade-ide/startup-project.json` as single `StartupProjectRelativePath`. F5 and interactive `debug_launch` resolve **one** target; MSBuild configuration for debug is effectively **Debug** in the resolver.

In Visual Studio and SDK projects the familiar model is **several named profiles** — different projects, configurations (Debug/Release/…), command-line args, working directory, environment variables. Developer switches “current profile” and presses F5 without resetting startup project each time.

Without this Cascade IDE stays closer to “one start per solution”, weak for monorepos, multiple executables in one `.sln`, and “run API / console / test host” without a file dialog.

**Web (ASP.NET Core)** is not “later”: typical portal/SPA-host solutions (including product **EDW.Portal**, scope `portal` in operational memory) depend on `Properties/launchSettings.json`: Kestrel **URLs**, `ASPNETCORE_ENVIRONMENT`, sometimes `launchBrowser`, profiles (`http` / `https` / IIS). Without mapping these fields into DAP launch (process env + host args if needed) F5 in CIDE **will not** match familiar `dotnet run` / VS. Console exe is a subset; **web is mandatory** v1 horizon per this ADR.

<a id="adr0090-decision"></a>

## Decision (direction)

Introduce a **launch profile catalog** in the workspace zone, with **currently selected profile**, and run debug (DAP launch) **through the active profile**, not sole `StartupProjectRelativePath`.

<a id="adr0090-profile-model-v1"></a>

### Minimal profile model (v1)

Per profile, at minimum:

- **Id** — stable name within solution (display name may match or localize separately).
- **Project** — path to `.csproj` / `.fsproj` **relative to solution root** (same base as `BreakpointsFileService.GetWorkspaceRoot(solutionPath)`).
- **MSBuild configuration** — e.g. `Debug` / `Release` (string; default `Debug` for debug).
- **Program arguments** — optional, string list or quoted string rules (refine at implementation).
- **Working directory** — optional; if empty — inherit from [IdeDapDebugSession](../../../Services/IdeDapDebugSession.cs) (`ResolveLaunchWorkingDirectory` logic exists).

- **Process environment variables** — for web at minimum merge what profile supplies (often `ASPNETCORE_ENVIRONMENT=Development`). Implementation: env in DAP `launch` (extend [IdeDapDebugSession](../../../Services/IdeDapDebugSession.cs) if missing), no silent drop.

<a id="adr0090-aspnet-v1"></a>

### ASP.NET Core / web (v1, not deferred)

Profile must express what is already in `launchSettings.json` for `commandName: Project` (Kestrel):

- **`applicationUrl`** — one or several bindings (`;` as in SDK template or URL array in TOML); on launch set in environment as `dotnet` does (typically `ASPNETCORE_URLS` or host-aligned — refine with `dotnet`/hosts).
- **`launchBrowser`** — optional; if `true`, open URL after start (CIDE wrapper if added; else mark deferred/v2 explicitly).
- Separate profiles like `http` / `https` — multiple named entries in TOML, 1:1 import from `launchSettings.profiles` where possible.

**IIS Express** (`commandName: IISExpress`) and full VS IIS parity — **may** be separate phase; ADR minimum: Kestrel + `Project` — web v1 baseline; IIS — best effort or separate ticket after baseline.

**Import:** reading web project `Properties/launchSettings.json`, TOML in `.cascade-ide` must **preserve URL and env semantics** so portal-style scenarios do not break vs `dotnet run --launch-profile …`.

<a id="adr0090-storage"></a>

### Storage

- **Canonical format: TOML**, not JSON — e.g. `.cascade-ide/launch-profiles.toml` (plus schema version key, e.g. `version = 1`). Same philosophy as user/canonical IDE config ([0028](0028-user-settings-toml-localappdata-and-secrets.md), [0029](0029-configuration-toml-canonical-ui-facade.md)): readability, comments, one style with `settings` / `workspace` beside repo.
- **One file** per open solution (path to `.sln` / standalone `.csproj` — as for breakpoints and startup): **profile list** and **active** profile (`active_profile` or equivalent), no second file to desync.
- **JSON** (`Properties/launchSettings.json`) remains external de-facto SDK/VS standard — **not** duplicated as primary canon in `.cascade-ide`; **import/export** into TOML model.

**Migration:** if only `startup-project.json` exists, on first read build **one** default profile (name like `Default` / from project name), set active, write `launch-profiles.toml`, keep old JSON for strangler until explicit removal.

<a id="adr0090-ui"></a>

### UI

- Visible **current profile selector** (toolbar or explorer strip / debug banner) — **keyboard-first** per [0013](0013-command-surface-and-discoverability.md).
- Commands: manage profiles (add/delete/duplicate) — MFD or modal by volume ([0074](0074-settings-ui-mfd-compact-layout-overflow.md) overflow policy).
- F5 / “start debugging” uses **active profile** without `.dll` dialog when resolve succeeds ([0002](0002-debug-human-agent-parity.md) parity).

<a id="adr0090-mcp-parity"></a>

### Agent parity (MCP / IdeCommands) — v1 contract

Debug launch contract at `IdeCommands.DebugLaunch` and `MCP-PROTOCOL`:

- Command: `debug_launch`.
- Mode A (explicit target): `workspace_path` + `target_path` (as today, backward compatible).
- Mode B (profile): `profile_name` (optional) + open workspace/solution context.
  - if `profile_name` omitted, use `active_profile`;
  - if profile not found — explicit contract error (no silent random startup fallback).
- Additional: `netcoredbg_path`, `program_args`.
- If both `target_path` and `profile_name` — **`target_path` wins** (explicit agent path stronger than profile).

Human and agent share F5/Launch meaning: “launch by active profile”; direct `target_path` kept for point scenarios.

<a id="adr0090-dotnet-alignment"></a>

### .NET alignment

- **Optional import** from `Properties/launchSettings.json` (after v1) — less friction for `dotnet`/VS projects. Canon remains **TOML in `.cascade-ide`**; **semantic compatibility** with standard `launchSettings`, not bitwise file identity.
- Optional **export** to `launchSettings`-like form for other tools — not mandatory.

<a id="adr0090-build-resolve"></a>

### Build resolve

- [MsBuildDebugTargetResolver](../../../Services/MsBuildDebugTargetResolver.cs): pass **configuration** from profile (`-p:Configuration=...`), not only constant `Debug`, when active profile is source of truth.

<a id="adr0090-consequences"></a>

## Consequences

- Refactor `MainWindowViewModel` / `StartupProjectStore` toward **“set + active”** model; old APIs — strangler.
- Tests: TOML parse, migration from single `startup-project.json`, import **web** `launchSettings`, F5 scenarios: console + **Kestrel** with valid URL.
- User Guide — product layer, not mandatory ADR volume (see [README](README.md)).

<a id="adr0090-rejected"></a>

## Rejected / deferred alternatives

- **Only** read `launchSettings.json` without own file — rejected: worse for IDE MVP parity (monorepos, paths relative to solution, stable agent contract in `.cascade-ide` beside breakpoints).
- **Multiple parallel debug sessions** in one IDE — out of scope (one DAP session unless separately fixed).

<a id="adr0090-implementation-status"></a>

## Rollout status

- Storage canon: `.cascade-ide/launch-profiles.toml`, migration from `startup-project.json`, `MsBuildDebugTargetResolver` with profile configuration, DAP `launch` with `env` and optional cwd, `debug_launch` with `profile_name` and explicit contract errors; tests `LaunchProfilesStoreTests`, docs `IdeCommands` / `MCP-PROTOCOL.md`.

<a id="adr0090-implementation-checklist"></a>

## Implementation checklist (decision → code)

1. Introduce `.cascade-ide/launch-profiles.toml` (profiles + `active_profile`) and migration from `startup-project.json`.
2. Update debug resolve: `MsBuildDebugTargetResolver` gets `Configuration` from profile.
3. Update `debug_launch` pipeline: `profile_name`, clear errors (`profile_not_found`, `active_profile_missing`, `profile_target_unresolved`), compatibility with `workspace_path + target_path`.
4. Sync docs: XML-doc `IdeCommands.DebugLaunch`, `docs/MCP-PROTOCOL.md`, ADR index links if needed.
5. Test plan: console profiles Debug/Release with different args; ASP.NET Core import `applicationUrl`/env and launch by profile; negative: missing profile and unresolvable target give explicit errors.
