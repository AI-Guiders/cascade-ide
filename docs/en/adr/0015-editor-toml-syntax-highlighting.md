<!-- English translation of adr/0015-editor-toml-syntax-highlighting.md. Canonical Russian: ../../adr/0015-editor-toml-syntax-highlighting.md -->

# ADR 0015: TOML highlighting in a text editor

**Status:** Accepted · Implemented  
**Date:** 2026-04-02

## Related ADRs

| ADR | Role |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | product data in TOML |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | `EditorLanguageSupport`, `TextMateTomlGrammar`, directory `TextMateGrammars/toml` |

---
## Context

The TOML product already has the **first class**: interface modes (`UiModes/`), user settings (`settings.toml`), loading via Tomlyn ([0010](0010-ui-modes-toml-configuration.md)). Users and developers open these files in the **built-in editor** of the Cascade IDE.

Syntax highlighting is provided through **AvaloniaEdit.TextMate** and **`RegistryOptions.GetLanguageByExtension`**, and the list of extensions is provided in **`EditorLanguageSupport`**. The built-in bundle **TextMateSharp.Grammars** **does not** contain** a separate grammar for `*.toml` (`GetLanguageByExtension(".toml")` → null without additional loading).

Separately: **Tomlyn** remains for **semantics** (deserialization, validation). It **doesn't** replace TextMate highlighting.

## Solution

<a id="adr0015-p1"></a>
1. **ADR area - highlight only (TextMate)** for `*.toml`. **Not included:** LSP, diagrams, formatting - separate ADRs/features.

<a id="adr0015-p2"></a>
2. **Grammar according to the TOML specification in TextMate format:** ships a VS Code-compatible package under **`TextMateGrammars/toml/`** (`package.json` + `syntaxes/toml.tmLanguage.json`). Current file is **MIT**, origin **taplo** (see `_source` in JSON and `TextMateGrammars/toml/README.md`). This is not a “write a grammar from scratch in a repo”, but a **supported artifact** in a standard format; if necessary, update - replacing the file from upstream (taplo / distribution like flox-vscode), while maintaining the license.

3. **Loading in runtime:** `RegistryOptions.LoadFromLocalFile` (available in **TextMateSharp.Grammars 2.x**; in the project there are explicit links `TextMateSharp` / `TextMateSharp.Grammars` **2.0.3** to override transitive 1.0.56 from AvaloniaEdit.TextMate) immediately after `new RegistryOptions(ThemeName.DarkPlus)` - **`TextMateTomlGrammar.TryLoadInto`**. The directory is copied to the assembly output (`CascadeIDE.csproj` → `Content`), the tests include the same directory (`CascadeIDE.Tests`).

<a id="adr0015-p4"></a>
4. **`EditorLanguageSupport`:** **`.toml`** in `Supported` (name **TOML**), in **`ExtensionToGrammarExtension`** - **`.toml` → `.toml`** (after loading the package `GetLanguageByExtension(".toml")` is not null).

<a id="adr0015-p5"></a>
5. **MCP/settings:** still from `Supported` without duplicate lists.

<a id="adr0015-p6"></a>
6. **Invariant:** test **`ExtensionToGrammarExtension_AllResolveInTextMateRegistry`** uses the same initialization order as the application (`TryLoadInto` before testing).

## Consequences

- The highlighting corresponds to **TOML as a language** (tables, strings, numbers, dates - according to the rules of grammar), and not to the approximation via INI.
- A small **spike bundle** next to the exe; grammar update - editing files + running tests.
- Expectations **"like taplo LSP in VS Code"** are still not stated: TextMate only.

## Perspective: smart TOML and external editor

For **the first time** highlighting in the built-in editor and validation when loading via **Tomlyn** ([0010](0010-ui-modes-toml-configuration.md)) is enough. Complex editing of `UiModes/`, `settings.toml`, etc. you can **consciously** run it in an **external editor** (VS Code, Cursor, ...) with the usual TOML tooling - this is not a drawback of the release, but a **conscious narrowing of the scope**, while there is no pain “we edit configs only from Cascade all the time.”

When there is a product need for **diagnostics, keymap, formatting, auto-completion** directly in the IDE - issue it as a **separate ADR** (LSP or in-process on Tomlyn, etc.), without the 0015 extension retroactively.

## Rejected alternatives

- **Tomlyn as a highlighting engine** - rejected (not tokenization for TextMate).
- **Mapping to INI** - used as a temporary bypass to the spiked grammar; for the final state it is replaced by [p. 2](#adr0015-p2)–[3](#adr0015-p3).
- **LSP TOML in this ADR** - rejected due to volume.

## Grammar update

See `TextMateGrammars/toml/README.md`: replace `toml.tmLanguage.json` from the selected upstream (MIT), correct `package.json` if necessary, make sure the resolve tests pass.