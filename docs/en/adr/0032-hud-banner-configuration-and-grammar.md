<!-- English translation of adr/0032-hud-banner-configuration-and-grammar.md. Canonical Russian: ../../adr/0032-hud-banner-configuration-and-grammar.md -->

# ADR 0032: HUD above editor - custom content and grammar (like `presentation`)

**Status:** Proposed (intention fixed; implementation according to plan).  
**Date:** 2026-04-11

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD - Cascade IDE Cockpit Attention Model |
| [0085](0085-editor-hud-inline-layer-and-hud-banner.md) | terms **Editor HUD** vs **HUD banner** - this ADR is about the **banner** config |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | line `presentation`, section `[presentation_grammar]`, EBNF to ADR |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `settings.toml` |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | canon on disk, UI as a façade |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | orthogonal to commands |

---
## Context

According to [0021](0021-pfd-mfd-cockpit-attention-model.md) **HUD** is a layer **inside the frontal** (editor), not the fourth spatial anchor; it includes inline diagnostics, ghost text, a bar above the text, etc. It's literally already been here; [0085](0085-editor-hud-inline-layer-and-hud-banner.md) does not change that canon, but gives the names **Editor HUD** (the entire layer) and **HUD banner** (only the file-level strip), so as not to identify the “HUD” with one strip. **This ADR** is about the customizability of the **banner**: now the **bar text** above the editor is set by code (summary of active file diagnostics, fixed wording) - see `MainWindowViewModel` (partial class with HUD).

In parallel, a pattern has already been adopted for **screen topology**: a line in the settings (`presentation` / `zone_screen_layout`) + **custom tokens** in TOML (`[presentation_grammar]`), a parser with tests, a specification in the form of EBNF in [0017](0017-multi-window-workspace-and-agent-surfaces.md).

Product idea: **put into the user config**, *what* and *in what form* is shown by the **HUD banner** (the bar above the editor), and if possible use **the same architectural technique** as for `presentation`: canon in `settings.toml`, optional grammar section, explicit semantics of data sources.

## Decision (intention)

<a id="adr0032-p1"></a>

1. **Storage layer:** as for `presentation`, description of the contents of **HUD banner** - **primarily** in the user **`settings.toml`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)); do not duplicate the “repository vs personal settings” logic differently than in existing ADRs according to the config.

<a id="adr0032-p2"></a>

2. **Two levels of adjustment (target minimum):**
   - **Semantics:** which **sources** can participate in the **HUD banner line** (for example, a summary of diagnostics for the active document, later - other signals), on/off, priority in case of conflict of area or attention - in the spirit of Dark Cockpit from [0021](0021-pfd-mfd-cockpit-attention-model.md) §9.
   - **Representation:** line template and/or mini-DSL for fragment order (placeholders like error/warning numbers, delimiters); The default human-readable strings are from the i18n layer ([0033](0033-internationalization-resx-avalonia.md)), not from the mandatory sheet in TOML.

<a id="adr0032-p3"></a>

3. **Grammar similar to `presentation`:** an optional section in TOML (working name - for example **`[hud_grammar]`** or consistent with the `CascadeIdeSettings` model) sets **literals and delimiters** if the **HUD banner** configuration line becomes parsed by a mini-language (brackets, gluing blocks, prohibiting “magic” symbols only in code). Defaults must match current behavior where it becomes canon.

<a id="adr0032-p4"></a>

4. **EBNF and implementation:** grammar **documented** in ADR (as for `presentation` in [0017](0017-multi-window-workspace-and-agent-surfaces.md)) for review and reference. **Implementation** - pragmatic: a small manual parser + tests, **for now** the language is narrow; connecting **generators from EBNF** or heavy parsing libraries - only if the DSL **expands** so much that manual support becomes more expensive than the dependency (similar to the rationale for `presentation`, where an explicit parser is already used rather than ANTLR).

<a id="adr0032-p5"></a>
5. **Boundaries:** this ADR **does not** change the semantics of attention zones ([0021](0021-pfd-mfd-cockpit-attention-model.md), [0025](0025-sdk-attention-zones-and-capabilities.md)) and **does not** promise a specific set of placeholders before design; **do not** mix the **HUD banner** config with window placement ([0017](0017-multi-window-workspace-and-agent-surfaces.md)).

## Consequences

- An explicit contract will appear in the code: settings model (`CascadeIdeSettings` or nested type), template/DSL parsing, parser tests and banner text regressions.
- User documentation (when there is a User Guide layer) will be able to refer to one canon in TOML instead of “as hardcoded in the assembly.”

## Rejected / deferred alternatives

- **Only UI without canonical on disk** - contradicts [0029](0029-configuration-toml-canonical-ui-facade.md); point UI is possible as a façade, but the source of truth is TOML.
- **The parser generator from EBNF** is redundant for the first iteration; review as DSL grows ([clause 4](#adr0032-p4)).

## Open questions

- Exact names of TOML keys and fields in `CascadeIdeSettings` (match snake_case and merge layer from [0028](0028-user-settings-toml-localappdata-and-secrets.md)).
- Do you need a **separate** template string or just structured boolean flags + one number formatting template?
- Default UI wording for HUD and other surfaces is from the localization layer ([0033](0033-internationalization-resx-avalonia.md)), rather than the mandatory long TOML translations.