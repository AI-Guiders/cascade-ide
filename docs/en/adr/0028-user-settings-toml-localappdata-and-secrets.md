<!-- English translation of adr/0028-user-settings-toml-localappdata-and-secrets.md. Canonical Russian: ../../adr/0028-user-settings-toml-localappdata-and-secrets.md -->

# ADR 0028: User settings - `settings.toml`, directory `%LocalAppData%\CascadeIDE\`, secrets separately

**Status:** Accepted · Implemented  
**Date:** 2026-04-08  
**Updated:** 2026-04-13 - LSP presets in `settings.toml` ([0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md)). Details - [§ History](#adr0028-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | TOML for **bundle modes** and **repository** `workspace.toml` is another layer, not to be confused with the user file |
| [0013](0013-command-surface-and-discoverability.md) | `hotkeys.toml` next to `settings.toml` - intended, not necessarily implemented |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | part of the presets in **merged** `workspace.toml`; custom placement override - not just `settings.toml` |
| [0027](0027-small-team-focus-vs-public-maturity.md) | predictable configuration paths |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML as canon; UI - facade above the same file |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation` - first of all **`settings.toml`**, not a repo |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | C#/Markdown LSP: presets, optional `executable`/`arguments` keys, optional environment |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| Service | `SettingsService` |
| Catalog | `%LocalAppData%\CascadeIDE\` |
| Settings | `settings.toml` (Tomlyn, snake_case) |
| Secrets | `ai-keys.toml` (separate from settings) |

## Summary

- Custom canon: **`%LocalAppData%\CascadeIDE\settings.toml`** (Tomlyn).
- Secrets - **`ai-keys.toml`**, not in the main settings file.
- Separate from the `UiModes/` bundle and merge `.cascade/workspace.toml` ([0010](0010-ui-modes-toml-configuration.md)).
- LSP presets - [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md).

---
## Context

The product already has **several layers** of configuration: spiked `UiModes/` bundle, merge with `.cascade/workspace.toml` in the open repository ([0010](0010-ui-modes-toml-configuration.md)), global user preferences (AI, MCP, LSP, panel visibility, UI locale, etc.). At the same time, there was no **separate ADR** that captures **user** channel (path on disk, format, what we put where) - the links are scattered across [0010](0010-ui-modes-toml-configuration.md), [0015](0015-editor-toml-syntax-highlighting.md), UX-docs.

We need **one canon point**: where “my computer” is located, what is in TOML, what cannot be put in TOML, how it relates to the repository layer.

---

## Solution

### 1. CascadeIDE User Data Directory

- **Base path:** `%LocalAppData%\CascadeIDE\` (via `Environment.SpecialFolder.LocalApplicationData` + `CascadeIDE` segment).
- The directory is **created** on the first call, if missing (`SettingsService.GetSettingsDirectory()`).
- This directory is **not** the solution directory and **not** `AppContext.BaseDirectory`; it is **tied to the Windows user** and the installation of the application on the machine.

### 2. Main settings file: `settings.toml`

- **Full path:** `%LocalAppData%\CascadeIDE\settings.toml`.
- **Model:** `CascadeIdeSettings` is the only source of truth for **set of fields** and meanings (AI providers, MCP, LSP C#/Markdown, Kroki, panel visibility, `UiMode`, UI locale, etc.).
- **Serialization:** Tomlyn via `CascadeTomlSerializer`: keys in file - **snake_case**, C# properties - PascalCase (`TomlSerializerOptions` with `JsonNamingPolicy.SnakeCaseLower`).
- **Loading:** when starting `SettingsService.Load()`; in case of a reading/parsing error - **default model** from the code, without crashing the IDE.
- **Saving:** `SettingsService.Save(CascadeIdeSettings)` - overwriting the entire file; write errors are **swallowed** (current implementation policy is not to block the UI).

**Before public release:** do not increase automatic migrations in `SettingsService` when renaming/transferring TOML keys - see subsection **"Before public release"** in the [root README](../../README.md#before-public-release). After the appearance of mass installations, a separate solution (file version, one-time migrator, changelog).

### 3. Historically: JSON is no longer supported
- Previously, the code had a one-time migration **`settings.json` → `settings.toml`**; As of this ADR, there are **no legacy settings supported**, migration branch **removed** from `SettingsService.Load()`.
- **Canon** - only **`settings.toml`**; if there is no file, factory defaults from embedded **`Settings/defaults-settings.toml`** (EmbeddedResource), then user overlay merge. The custom **`settings.json`** in `%LocalAppData%\CascadeIDE\` is **not** readable (if the file is left manually, you need to transfer it to TOML yourself or delete it).

### 4. Secrets: not in `settings.toml`

- **API keys** (Anthropic, OpenAI, DeepSeek, etc.) are stored in **`%LocalAppData%\CascadeIDE\ai-keys.toml`**, separate `AiKeys` model, serialized via **`CascadeTomlSerializer`** (same as `settings.toml`: keys in the file - **snake_case**), `AiKeysStorage.Load` / `Save`.
- **Reasons for separation:** do not reveal secrets in the same file, which is convenient to copy/display in logs; simpler backup policy and `.gitignore` for the user; corresponds to the direction [0013](0013-command-surface-and-discoverability.md) (do not mix everything in one “lump”). **TOML** format rather than separate JSON - single serialization stack with custom settings.
- **Do not commit** `ai-keys.toml`; The developer documentation assumes only a local machine. The file **`ai-keys.json`** is not readable (if it remains manually, move it to TOML or delete it).

### 5. What this ADR *doesn't* describe (explicit distinction from [0010](0010-ui-modes-toml-configuration.md))

| Layer | Where | Destination |
|------|-----|------------|
| User | `%LocalAppData%\CascadeIDE\settings.toml` | **User** preferences on this machine (model `CascadeIdeSettings`). |
| Application bundle | `UiModes/` next to exe | UI modes, index, **spike** `workspace.toml` bundle. |
| Repository | `<solution>/.cascade/workspace.toml` | Team/project overlay on top of the bundle (merge in [0010](0010-ui-modes-toml-configuration.md)). |

#### Name `workspace.toml`: one merge contract, not “two meanings” - and where the file is not

- **Chrome and mode presets ([0010](0010-ui-modes-toml-configuration.md)):** one data model (`UiWorkspaceToml`), two **sources** of one file name - spiked **`UiModes/workspace.toml`** and when the solution is open, optionally **`<repo>/.cascade/workspace.toml`**. This is a **merge chain** (bundle → overlay repo), and not two different products with the same name.
- **`%LocalAppData%\CascadeIDE\`:** there is no **`workspace.toml` file** and in the current canon **not intended** - global user settings live in **`settings.toml`**. A typical confusion when reading the docs: look for "user workspace" next to `settings.toml` under the same name.

**Renaming** `workspace.toml` in a bundle/repo (for example in `ui-workspace.toml`) **we do not do** just for the sake of uniqueness of the name on disk: it will affect the assembly, merge, keys in [0026](0026-markdown-preview-surfaces-and-placement.md), examples and expectations “workspace = IDE layout”. If ever needed - **separate ADR** + migration of paths and bundle versions.

The state tied to a **specific solution** (for example, the selected startup project for debugging: `Services/StartupProjectStore` → `<directory .sln>/.cascade-ide/startup-project.json`) **does not** lie in `%LocalAppData%\CascadeIDE\` - this is a separate channel "next to the repo", not global user settings.

### 6. Future files in the same directory

- **`hotkeys.toml`** - by intent [0013](0013-command-surface-and-discoverability.md), next to `settings.toml`, overrides only; prior to implementation, this ADR does not obligate their availability.
- Layout by **physical displays** (`presentation` / `zone_screen_layout` and grammar tokens by [0017](0017-multi-window-workspace-and-agent-surfaces.md)) - **personal** layer, **primarily** fields in **`settings.toml`**, rather than a team commitment via the repository `workspace.toml` ([0017 § storage layer](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-grammar)).
- Additional user files (for example, window status by [0017](0017-multi-window-workspace-and-agent-surfaces.md)) - **separate ADR** when an agreement appears, with an explicit link to this directory or to a new key in `settings.toml`.

---

## Consequences
- Documentation and agents: with the word “user settings” - the path to **`settings.toml`** and the model **`CascadeIdeSettings`**; for “API secrets” - **`ai-keys.toml`**.
- New fields in user settings are added to **`CascadeIdeSettings`** + if necessary UI/saving; when the meaning of a layer changes, **update this ADR** or refer to a new one.
- Tests can replace loading through existing mechanisms or model instances without writing to disk - without changing the canon of paths.

---

## Rejected alternatives

- **Keep custom settings in JSON as primary format** - rejected: canon - **TOML**; automatic migration from `settings.json` is **removed** from the code in the absence of supported legacy profiles.
- **Store API keys in `settings.toml`** - rejected: mixing secrets with portable/editable config and risk of leakage when exchanging files.
- **Keep secrets in a separate JSON (`ai-keys.json`)** - rejected in favor of **`ai-keys.toml`**: same format with `settings.toml` and the same `CascadeTomlSerializer`; the separate file still isolates the secrets from the "regular" config.
- **One ADR for all TOML** - rejected: [0010](0010-ui-modes-toml-configuration.md) remains about modes and merge; user file - separate path and content contract.
- **Rename `workspace.toml` in the bundle/repo only because the name matches expectations** - rejected without separate ADR and migration (see §5.1).

---

## History of changes

<a id="adr0028-history"></a>

| Date | Change |
|------|-----------|
| 2026-04-08 | Canon: `settings.toml`, LocalAppData directory; `workspace.toml` in the merge layer ([0010](0010-ui-modes-toml-configuration.md)). |
| 2026-04-08 | Removed `settings.json` → TOML migration; legacy is not supported. |
| 2026-04-08 | Secrets: `ai-keys.toml` instead of JSON; There is no migration from JSON. |
| 2026-04-11 | Before public release - without auto-migrate scheme ([README § Before public release](../../README.md#before-public-release)). |
| 2026-04-11 | `presentation` → [0017](0017-multi-window-workspace-and-agent-surfaces.md). |
| 2026-04-13 | LSP in `settings.toml` → [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md). |