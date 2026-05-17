# ADR 0033: Internationalization (i18n) — .NET resources, Avalonia, orthogonal to TOML

**Status:** Proposed (direction fixed; language scope and string migration — per plan).  
**Date:** 2026-04-11

## Related ADRs

| ADR | Role |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | user `settings.toml` |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | configuration canon vs UI |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD: templates and keys — not a substitute for i18n |
| [0027](0027-small-team-focus-vs-public-maturity.md) | documentation and discoverability |

---
## Context

UI and message text is still mostly **hardcoded** in code (including Russian strings). For **multiple languages**, runtime language switch, and a proper translation pipeline we need **stable rules**, not duplicating long phrases in **`settings.toml`** or only in VMs.

Confusion to remove early:

- **Configuration** ([0028](0028-user-settings-toml-localappdata-and-secrets.md), [0029](0029-configuration-toml-canonical-ui-facade.md)) — canonical keys, numbers, flags; UI language is a **setting**, but **not** storage for all translated product strings.
- **HUD and templates** ([0032](0032-hud-banner-configuration-and-grammar.md)) — semantics of “what to show” may live in TOML; **user-facing wording** should by default come from the **localization layer**, not an optional wall of text in config.

## Solution (intent)

<a id="adr0033-p1"></a>

1. **Base stack:** **.NET embedded resources (`.resx`)** and an Avalonia-aligned scheme: resources as string source for XAML and code, setting **`Culture`** / `CurrentUICulture` at startup and on user language change; resource generator (e.g. `PublicResXFileCodeGenerator`) — per repo convention so keys are available from markup and tests.

<a id="adr0033-p2"></a>

2. **Role separation:**
   - **Translatable UI strings** — in **resources** (per culture: `Resources.resx`, `Resources.ru.resx`, …). Key catalog stable for translators and CI.
   - **`settings.toml`** — **not** the main translation file for the whole UI; **selection keys** allowed (e.g. preferred culture), optional **overrides** of individual texts for power users — only if explicitly designed and without breaking the unified translation catalog (see [§4](#adr0033-p4)).

<a id="adr0033-p3"></a>

3. **Code and MVVM:** strings built in **ViewModel** / services via the **same resource layer** or an abstraction like **`IStringLocalizer<T>`** / `ResourceManager` wrapper — to avoid literal sprawl and ease tests (culture substitution in tests).

<a id="adr0033-p4"></a>

4. **Plural, case, gender:** do not hand-code “Russian rules” in every VM. Prefer **separate resource keys** for variants (`*_one`, `*_few`, `*_many` / language-specific scheme) or a **proven library** with CLDR/ICU rules — choice when implementing the first non-English language with non-trivial plural rules; until then simple keys suffice.

<a id="adr0033-p5"></a>

5. **Boundaries:** this ADR **does not** require immediate migration of all existing strings; **does not** replace configuration ADRs; **does not** describe the product User Guide ([0027](0027-small-team-focus-vs-public-maturity.md)) — only **in-app UI**.

## Consequences

- `*.resx` structure, key naming policy, possible CI step “no missing keys”.
- Runtime language change will require binding/VM notifications where text is not from `{x:Static}`.

## Rejected / deferred alternatives

- **All strings only in TOML** — poor for translation, duplication, risk of editing “in config” instead of catalog; contradicts [0029](0029-configuration-toml-canonical-ui-facade.md) spirit as “settings”, not full product dictionary.
- **Third-party format only (gettext, Fluent)** without evaluation — deferred; migration possible if ResX stops fitting the team or translation tools.

## Open questions

- **Target languages** for v1 and order (infrastructure + English first, then Russian etc.).
- Single **key prefix** or split by assemblies/features (`Features/*`).
- Integration with **command palette** and XML docs [0018](0018-ide-commands-canonical-xml-documentation.md) — how not to diverge hint languages.
