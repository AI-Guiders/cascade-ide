<!-- English translation of adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md. Canonical Russian: ../../adr/0040-lsp-launch-line-settings-toml-presets-and-environment.md -->

# ADR 0040: LSP (C# / Markdown) - command line in `settings.toml`: presets, optional keys, override via environment

**Status:** Accepted · Implemented (as [§Solution](#solution) below)  
**Date:** 2026-04-13; updated 2026-04-25 - TOML canon: `[languages.csharp]` / `[languages.markdown]` + `mode` + nested profiles

## Related ADRs

| ADR | Role |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | where is `settings.toml`, snake_case, model `CascadeIdeSettings` |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML as canon; UI - façade |
| [0023](0023-environment-readiness-glance.md) | quick tips on LSP without `environ` dump |
| [0023](0023-markdown-diagrams-language-tooling.md) | Markdown as first-class; LSP for the long term |

---
## Context

1. In `%LocalAppData%\CascadeIDE\settings.toml` language LSPs are configured in **`[languages.csharp]`** and **`[languages.markdown]`**: **`mode`** discriminator (for example `ParseOnly` / `OmniSharp` / `Marksman`) and **nested tables** profiles with **`executable`** / **`arguments`** for stdio.
2. For built-in presets, the user often does not duplicate empty lines in the file “as in the example” - `mode` is enough and, if necessary, overrides in the corresponding nested table.
3. A separate need is to **not store** absolute paths to tools in `settings.toml` (common dotfiles repository, CI, different machines), but to have a **predictable** way to substitute them from the environment without the “magic” in the `executable` value.

---

## Solution

### 1. Canon for today (implemented): `mode` and nested profiles

- The resolution of the **process file + arguments** pair is taken from the **active profile** by `mode` (see `ResolveForRuntime()` in `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings`).
- For presets with reasonable default on **PATH** empty or space-only **`executable`** in profile means: use **built-in command name** (e.g. `marksman`, `csharp-ls`, `OmniSharp` with fixed argument prefix `--languageserver` - see code) rather than "configuration error".
- Empty **`arguments`** is acceptable: additional arguments are added to the preset only if the user specifies a non-empty string.
- The **`[languages.csharp.custom]`** profile (and Markdown analog) still requires a non-empty **`executable`** if there is no built-in default for that path; this does not change with the ADR data.

### 2. Canon for today: optionality of keys in TOML

- The **`executable`** and **`arguments`** keys **do not have to** be present in the nested profile table if the default values from the model (empty lines) are acceptable and the selected `mode` supports the preset from step 1.
- Example of a minimal Markdown LSP configuration:```toml
[languages.markdown]
mode = "Marksman"
```
(table `[languages.markdown.marksman]` with empty `executable`/`arguments` - optional for clarity.)

- Example with C# (ParseOnly is the default, you don’t have to specify `mode` in the file if you like):```toml
[languages.csharp]
mode = "ParseOnly"

[languages.csharp.parse_only]
executable = ""
arguments = ""
```
### 3. Suggestion for the future (not necessarily implemented): explicit flag “from the environment”

**Purpose:** to provide an option to **not duplicate** the path in TOML and still explicitly tell the IDE to “take from environment variables” rather than relying on an accidental empty string.

- In the **profile** (or in the general LSP block) a Boolean field is entered, for example **`launch_from_environment`** (snake_case in the file, PascalCase in the model - like the rest of `settings.toml` by [0028](0028-user-settings-toml-localappdata-and-secrets.md)).
- When **`launch_from_environment = true`**:
  - **`executable`** and **`arguments`** in TOML **may be absent** or empty without loss of meaning;
  - The IDE reads **consistent** variable names (prefix, e.g. `CASCADE_IDE_...`, and suffixes by LSP view) documented in this ADR and in the readiness shortcut ([0023](0023-environment-readiness-glance.md)).
- **Priority** after implementation must be recorded in the code and here; recommended order: values ​​from the environment **only if** true flag; otherwise - as now (TOML → preset). If the flag is true and there are **missing** variables - either rollback to the preset as in step 1 (with an explicit line in readiness), or “do not start” with an explicit reason; **not** silent mixing.
- The names of environment variables are specified as a **separate sub-clause** of this ADR during the first implementation (so as not to block the adoption of clauses 1–2); Until implementation, the extension status remains **Proposed**.

### 4. Rejected alternatives

- **Magic substitutions** inside the `executable` string (`$ENV`, `${VAR}` without a separate flag) - rejected: worse to explain, more difficult to validate and show in readiness.
- **One global flag** for both LSPs without section binding - rejected: confusion with variable names and "Markdown only on CI" scripts.

---

## Consequences

- Documentation and examples `settings.toml` reduces LSP sections to `mode` and optionally **one** nested profile table; full `executable`/`arguments` remain valid for explicitness.
- When adding **`launch_from_environment`** - update `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings` models (and if necessary `ResolveForRuntime` wrappers), launch hosts, **Environment readiness** and example in `docs/samples/settings.toml`; change the extension status in the ADR header to **Accepted** after the review.
- [0028](0028-user-settings-toml-localappdata-and-secrets.md) does not duplicate LSP semantics - for command line and env details, link **here**.

---

## Implementation status (reconciliation)

| Part ADR | Code/artifacts |
|-----------|-----------------|
| Presets, empty `executable` | `CSharpLspProviderIds`, `MarkdownLspProviderIds`, `LanguageServerLaunchProfile` |
| Optional keys in TOML | Deserialize `CascadeIdeSettings` + default values ​​in `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings` |
| `launch_from_environment` | **not yet** in the model and resolver - **Proposed** |