# ADR 0109: Unified declarative catalog of parametric Intent Melody (TOML + code binding of args)

**Status:** Accepted · Implemented  
**Date:** 2026-05-11  

## Related ADRs

| ADR | Role |
|-----|------|
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | Parametric tails for editor line ranges |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Command Melody, CascadeChord, parity with `c:` |
| [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Web portal and parametric melody `wai:`… |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id`, registry layers |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP / command execution |

## Summary

- **Intent Melody catalog:** `[[melody_root]]` + `[[tail_wire_class]]` in TOML.
- Migration from legacy `[aliases]` / `[[parametric]]`; binders in code.
- Implemented: loader, runtime catalog, palette/chord; plugins — separate model.

### Implementation snapshot

| Element | Value |
|---------|----------|
| TOML | `[[melody_root]]`, `[[tail_wire_class]]` |
| Docs | `intent-melody-language-v1`; no legacy `[aliases]` |

### Outside ADR

| Document | Role |
|----------|------|
| [intent-melody-language-v1.md](../../intent-melody-language-v1.md) | IML v1 |
| [`IntentMelody/intent-melody-aliases.toml`](../../../IntentMelody/intent-melody-aliases.toml) | Registry alias → `command_id` |

**On IML:** this ADR defines an architectural path to **IML “v2”** not as a new language from scratch but as a **natural extension of v1**: the same wire for the user (`wai:…`, `els:…`), but a **unified declarative root catalog** (`[[melody_root]]`), explicit **`shape`**, presentation (**`[[tail_wire_class]]`**), and deterministic args assembly in code. A normative “IML v2” document may be written separately after fields stabilize; until then the source of truth is here and updates to [intent-melody-language-v1.md](../../intent-melody-language-v1.md).

---

## 1. Context

Today parametric melody forms are **scattered**:

- A flat **`[aliases]`** table in TOML only maps **short mnemonic → `command_id`** (`IntentMelodyAliases`).
- Special cases are **hard-coded** in **`ParametricIntentMelody`** (parsing `wai:…`, `els:start:end`, “palette only” lists, chord heuristics, hints).
- The palette (**`IdeCommandPaletteFilterOrchestrator`**) and chord (**`CascadeChordIntentSession`**) must **duplicate semantics** of what is parametric, when Enter fires, and which arguments apply.

That scales poorly: each new parametric root needs edits in **several** places and behavior easily diverges between `c:` and Ctrl+K.

---

## 2. Problem

1. **No unified shape description**: what is the parametric “base”, tail syntax, whether chord needs **Enter**, how slots map to **JSON command args**.
2. **Alias declaration and parametric declaration are split** — conflicts with the IML principle (“one line — one intent” on top of the shared command canon).
3. **C# attributes like `[Parameter(Position, Type, Description)]`** on execution methods **do not match** the args assembly point: the command executor takes **`command_id` + JSON dictionary**; building that dictionary depends on **IDE context** (file, editor text, column normalization for ranges, etc.), not only CLR parameter DTOs.

Requirement: **one declarative source of truth**, while **complex semantics** stay in **small, named code**, not growing `switch`es on alias strings.

---

## 3. Decision

### 3.1. Two layers: catalog in TOML + binding to `command_id` in code

1. **Target root declaration — one array `[[melody_root]]`** ([§3.2.1](#adr0109-melody-root)): each row is **one** slug, **`command_id`**, **`shape = "simple" | "parametric"`**; for `parametric`, the same fields today split between `[aliases]` and `[[parametric]]` (`tail_signature`, `wire_class`, `chord_commit`, …). One loader pass, one DTO, **no duplicate** slug/`command_id` or table drift — **simpler long term**.

2. **TOML migration:** it is acceptable to **convert [`intent-melody-aliases.toml`](../../../IntentMelody/intent-melody-aliases.toml) to target **`[[melody_root]]`** ([§3.2.1](#adr0109-melody-root)) **in one manual edit** (or script) — a long transitional format is **not required**. If phased adoption is easier, the file may temporarily stay **flat `[aliases]` + `[[parametric]]`** (+ optional **`[alias_type]`**, **`[[tail_wire_class]]`**, [§3.2.2](#adr0109-toml-migration)); the loader **normalizes** to the same internal snapshot as from **`[[melody_root]]`**. All in **one** file (a second file remains the exception when volume grows).

3. **`tail_signature`** — one string in **canonical slot notation** after the root’s first **`:`**, see [§3.3](#adr0109-p-slot-notation). The loader **derives parse strategy** from that signature (no separate `syntax` in config). **Inter-slot presentation** (which symbols are allowed *between* slots in wire) — optional registry **`[[tail_wire_class]]`** in [§3.3](#adr0109-tail-wire-classes) — so alternatives like `:` **or** space are not hard-coded only in C#, but live beside the catalog in a narrow “literal **or** literal” form.

4. **No `binder` field in TOML.** The catalog describes only **tail shape**; mapping parsed slots to `args` is part of `command_id` execution and/or a thin `ParametricMelodyArgsBuilder` layer chosen **deterministically** by (`command_id`, `tail_signature`, IDE context), without “magic strings” in configuration.

Invariant: **`command_id`** remains the canonical execution key (as MCP); TOML must **not** become Turing-complete — only route string form → args assembly by deterministic rules in code.

<a id="adr0109-alias-type"></a>

#### Linking `[aliases]` and `[[parametric]]`: `alias_type` *(migration phase only)*

While the file still has flat **`[aliases]`** and separate **`[[parametric]]`**, not unified **`[[melody_root]]`**: short mnemonic in **`[aliases]`** (`slug → command_id`), tail shape in **`[[parametric]]`** with the same **`alias`**. After moving to **`[[melody_root]]`**, separate **`[alias_type]`** is unnecessary — **`shape`** plays that role. Below — rules for the transitional format.

To align loader and humans on whether a target is **simple** (no tail parse) or **parametric**, introduce **`alias_type`**:

| Value | Meaning |
|----------|--------|
| **`simple`** | A single line without slot parse after the root is enough to execute (like most `c:` roots today). |
| **`parametric`** | This slug requires a **`[[parametric]]`** row with matching **`command_id`** and shape fields (`tail_signature`, …). |

**Two modes.**

1. **Default — inference:** if **`alias_type` is empty** (or missing for a slug), type is **`parametric`** when **`[[parametric]]`** exists for the same **`alias`**, else **`simple`**. One file and one load pass suffice; duplicate is optional.
2. **Explicit mode:** optional **`[alias_type]`** table with the same keys as **`[aliases]`** and values **`simple`** / **`parametric`**. Use for **strict validation** (explicit `parametric` without `[[parametric]]` → dev error; `simple` with `[[parametric]]` for the same slug → error or overlap forbidden — loader policy) and so diffs clearly show row class without reading the whole `[[parametric]]` array.

If you keep the temporary layout in [§3.2.2](#adr0109-toml-migration), flat **`[aliases]`** need **not** disappear immediately — compatibility with the accepted file format; **`[alias_type]`** is added only when explicit validation is needed. On **manual** migration to **`[[melody_root]]`** ([§3.2.1](#adr0109-melody-root)), neither **`[alias_type]`** nor **`[[parametric]]`** are needed for the same roots.

Consistency invariant: for each slug, **`command_id`** in **`[aliases]`** and in matching **`[[parametric]]`** (if any) **must match**.

### 3.2. TOML record shapes

Field illustration *(contract for the loader; file schema version keys — see unification note below)*. **`tail_signature`** meta-notation — [§3.3](#adr0109-p-slot-notation). On migration to **`[[melody_root]]`**, schema version keys (**`melody_catalog_schema_version`** vs **`parametric_schema_version`**) should **collapse to one key** so the loader reads a single version.

<a id="adr0109-melody-root"></a>

#### 3.2.1. Target unified model (long term): `[[melody_root]]`

One array for the whole `c:` root catalog: **slug**, **`command_id`**, **`shape`**. For **`shape = "parametric"`** — shape fields; for **`simple`** — only slug and `command_id` (and rare shared UX fields if needed). No duplicate slug across tables.

Optional **`show_usage_hint_if_bare_slug`** (bool): if **`true`**, in palette **`c:`** typing only the root **without** args shows a **tail-form hint line**, not the catalog command row (**`els`/`eld`** and stubs may default **`true`** in the loader). If **`false`** (or omitted where default is false), bare slug behaves like a normal palette command when resolved.

```toml
melody_catalog_schema_version = 1

[[tail_wire_class]]
id = "url_remainder"
kind = "single_remainder"

[[tail_wire_class]]
id = "int_chain_colon_space"
kind = "delimited_slots"
between_slots_any_of = [":", " "]

[[melody_root]]
slug = "br"
command_id = "build_solution_ui"
shape = "simple"

[[melody_root]]
slug = "wai"
command_id = "show_web_ai_portal_page"
shape = "parametric"
tail_signature = "<url:url>"
wire_class = "url_remainder"
chord_commit = "enter"
palette_hint_slug = "wai-url"
show_usage_hint_if_bare_slug = false

[[melody_root]]
slug = "els"
command_id = "select"
shape = "parametric"
tail_signature = "<start:ln>:<end:ln>"
wire_class = "int_chain_colon_space"
chord_commit = "enter"
show_usage_hint_if_bare_slug = true
```

<a id="adr0109-toml-migration"></a>

#### 3.2.2. Optional: flat `[aliases]` + `[[parametric]]` (phased adoption)

If target **`[[melody_root]]`** is not introduced in one commit (e.g. loader learns the old file shape first), the **previous** layout is allowed temporarily (plus optional **`[alias_type]`** — [§3.1](#adr0109-alias-type)). Otherwise **skip this block** — go straight to [§3.2.1](#adr0109-melody-root).

```toml
parametric_schema_version = 1

[aliases]
wai = "show_web_ai_portal_page"
els = "select"
eld = "apply_edit"

[alias_type]
wai = "parametric"
els = "parametric"
eld = "parametric"

[[tail_wire_class]]
id = "url_remainder"
kind = "single_remainder"

[[tail_wire_class]]
id = "int_chain_colon_space"
kind = "delimited_slots"
between_slots_any_of = [":", " "]

[[parametric]]
alias = "wai"
command_id = "show_web_ai_portal_page"
tail_signature = "<url:url>"
wire_class = "url_remainder"
chord_commit = "enter"
palette_hint_slug = "wai-url"

[[parametric]]
alias = "els"
command_id = "select"
tail_signature = "<start:ln>:<end:ln>"
wire_class = "int_chain_colon_space"
chord_commit = "enter"

[[parametric]]
alias = "eld"
command_id = "apply_edit"
tail_signature = "<start:ln>:<end:ln>"
wire_class = "int_chain_colon_space"
chord_commit = "enter"
```

**`chord_commit`** fields normalize “instant on obvious single root vs Enter only” without a separate C# `HashSet`.

<a id="adr0109-p-slot-notation"></a>

### 3.3. Canonical meta-notation: `<name:type>` and clean TOML

To define parameters **unambiguously** in the catalog without nested-table clutter, fix a **string tail signature** after `alias`:

- Each slot is **`<identifier:type>`**.
- Multiple slots are separated by **one** literal **`:`** *between* closing `>` and the next `<`.

**Illustrative full forms (document canon / “how a human reads it”; `c:` prefix in palette is outside):**

| Canon example (tail after `c:`) | Meaning |
|----------------------------------|--------|
| `wai:<url:url>` | One parameter **`url`** of type **`url`**. |
| `els:<start:ln>:<end:ln>` | **`start`** and **`end`** — line numbers (1-based inclusive); slot type **`ln`** (or `linenumber`) in meta-notation, see [0081](0081-parametric-intent-melodies-editor-line-ranges.md). Slot **`:int`** for the same chain is still supported as equivalent at parse time. |

**`tail_signature`** in TOML carries **only** the part after the root (**without repeating the slug**): e.g. `"<url:url>"` or `"<start:ln>:<end:ln>"` — see [§3.2](#32-toml-record-shapes) (`[[melody_root]]` or `[[parametric]]`).

**Types** (`url`, `int`, later `path`, `string`, …) — **closed registry** in code: unknown type at catalog load → configuration error (dev) or ignore overlay row with log (product — separate decision).

**Wire compatibility.** User input stays simple: **`wai:https://…`** and **`els:5:15`** (no `<` `>`). `tail_signature` is for **description** (readability, hints, validators, args mapping) only; users do not type it.

<a id="adr0109-wire-separators"></a>

#### Wire between slots: not one separator for the whole language, not one per melody

One **global** inter-slot separator for all roots conflicts with natural wire: after `wai:` the whole tail is one URL with its own `:`, which cannot be split with the same symbol as an `els` chain. A **per-`[[parametric]]` `separator`** field is flexible but becomes a zoo for users and docs (“here `:`, there space”).

**Acceptable compromise — three levels** (per-row `separator` is **not** baseline; presentation rules for a *form class* live in the bundle beside the catalog and/or minimal code fallback, see [§3.3](#adr0109-tail-wire-classes)):

1. **One “free” slot (remainder of line)** — e.g. `<url:url>` without a second slot: after canonical `alias:` (**first** literal `:` in wire separates root from value; further `:` inside the value are not inter-slot. Spaces, brackets, Cyrillic in the same value are allowed; URL validity — after the parser.)
2. **Simple slot chain** from a **closed type set** (e.g. `int` and **`ln`** / `linenumber` for consecutive line numbers): inter-slot separator comes from a **form class** (`kind = "delimited_slots"` + **literal alternatives** in TOML or inference from `tail_signature`), not a per-melody string. For **v1 backward compatibility**, several **equivalent writings** of the same semantics may be allowed (e.g. **`els:5:15`** and **`els 5 15`**), normalized before parse; docs and hints still show **one canonical** form.
3. **Exotic** — composite payload where boundaries cannot be told from `:`, multiple named segments with different delimiters, etc.: only then **`wire_profile`** (or explicit override) and when a real scenario appears — short follow-up ADR; until then stay on levels 1–2.

**Colons inside a single `url` slot** are covered by level 1. **Multiple URLs in one wire** without “whole remainder” is level 3 — explicit profile or separate ADR.

<a id="adr0109-tail-wire-classes"></a>

#### Registry `[[tail_wire_class]]`: presentation grammar between slots

To avoid a “symbol table **only** in code”, the same catalog file (or bundle block before `[[parametric]]`) can describe **wire classes** in a narrow *presentation* language: essentially **literal or literal** between slots, not full EBNF.

- **`id`** — class name; referenced by **`wire_class`** in **`[[parametric]]`** if you do not rely only on auto-inference from `tail_signature`.
- **`kind`**: e.g. **`single_remainder`** (one slot — whole tail after first root separator) or **`delimited_slots`** (slot chain; only listed separators between adjacent values).
- **`between_slots_any_of`** — array of strings (often **one character** each): in TOML cleaner than mixing with `|` in one field; **semantically** *literal alternatives* at the presentation layer (minimal “symbol or symbol” without full EBNF); on ambiguity **loader policy** chooses (see note below).
- **`kind`** and allowed fields tie to a **closed registry** in code: code remains the **executor** of this mini-descriptor (few `kind` templates), not a duplicate of every **`[[parametric]]`** row.

**Declarative intent is preserved:** melody inventory and **semantic** shape stay in **`[[parametric]]`** (or **`[[melody_root]]`**); **inter-slot symbol class** is a separate small data layer, overridable like the catalog. Args assembly after parse — still in code by (`command_id`, `tail_signature`, IDE context).

**Note.** Order of trying alternatives and normalization (` ` ↔ `:` etc.) are fixed by loader and tests; for MVP a unique integer pair after normalization is enough.

Meta-notation is **compact in TOML** as one **`tail_signature`** string, names parameters for hints and MCP args binding; substantive assembly after parse — in code, deterministically by (`command_id`, `tail_signature`, IDE context), see [§3.1](#31-two-layers-catalog-in-toml--binding-to-command_id-in-code).

### 3.4. Rejected alternatives

| Alternative | Why not as the only layer |
|--------------|----------------------------------|
| **C# attributes only** | Loses overlay without rebuild; parametric form is about **string language** and palette, not CLR signature; IDE context cannot be fully declarative. |
| **Code only (`switch` on alias)** | Observed coupling between palette, chord, and MCP; sync errors. |
| **One huge TOML without code** | Editor column math, file bounds, complex URL normalization must live in **tested code**. |

Acceptable **compromise**: TOML = **shape** (`tail_signature`); IDE core = **parse + deterministic args assembly** for built-in `command_id`; complex forms and plugins — see [§3.5](#35-where-toml-is-enough-and-where-xml-or-code-only) and [§3.6](#adr0109-p-plugins).

### 3.5. Where TOML is enough, and where XML (or code only)

Distinguish **parametric catalog entries** from **parse grammar / rule tree**.

1. **`tail_signature`** and other scalars (**`chord_commit`**, flags, hints) — **in TOML** on one catalog row (**`[[melody_root]]`** target or migration **`[[parametric]]`**), without growing tables: slot names and types are **embedded** in **`tail_signature`** ([§3.3](#adr0109-p-slot-notation)).

2. **Full grammar** (alternatives, branch priority, nested constructs, “this prefix first else that”) in **pure TOML** quickly becomes unreadable nesting or repeated lists.

3. When grammar is truly **branchy** and should be **edited without recompile** or **schema-validated**, a **hierarchical format** fits — often **XML** (or JSON/YAML + JSON Schema): sequence/choice, optional XSD/RNG. TOML then **references** a resource, e.g. `grammar_ref = "bundled:melody/strategies/foo.xml"` or path relative to overlay — one level of indirection, no catalog duplication.

4. **While the strategy family is small**, parsing **`tail_signature`** in code + deterministic args assembly is enough; moving parse to XML is **not required** until many parsers or an **external** rule editor is needed.

Summary: **TOML holds the catalog** and compact **slot signature** (`tail_signature`); **body of a complex strategy** — as complexity grows either **code** or an **external declarative file** (XML a reasonable candidate for deep nesting); mixing heavy grammar and TOML in one blob is not the goal of this ADR.

<a id="adr0109-p-plugins"></a>

### 3.6. Extensibility: plugins and who carries code

A catalog without `binder` in TOML targets **core**: fixed slot types and a table mapping (`command_id`, `tail_signature`) → args assembly.

When **plugins** appear, parametric extension is a **plugin boundary**, not “another magic TOML string”:

1. A plugin registering a **new** `command_id` and/or melody must provide handling for what it adds: at minimum parse/validate tail wire and build arguments for its command (or explicit failure with a clear error).
2. TOML may still declare **`slug`** / **`alias`**, **`command_id`**, **`tail_signature`** — as an **intent contract** (in target form, fields on **`[[melody_root]]`**), but “string → args” for a plugin stays **in plugin code**, not in core config without an extension host.

So there is no “guess a constant in TOML”: core has an unambiguous table; plugins contract through an extension API (modularity details — separate ADR when plugins are real).

---

## 4. Consequences

**Positive**

- One place to add a root long term: one **`[[melody_root]]`** row (`shape = "parametric"`) + a new args-assembly handler in code when needed; during migration — equivalent **`[[parametric]]`**. Inter-slot presentation in **`[[tail_wire_class]]`**, without `separator` on every melody.
- Unified rules for **palette `c:`**, **CascadeChord (Ctrl+K)**, and any other transport of the same string.
- Easier **IML documentation**: parametric shape beside alias in data, not spread through `ParametricIntentMelody.cs`.

**Negative / risks**

- **Migration** of existing `ParametricIntentMelody` (`PaletteOnlyAliases`, hard-coded `wai`, hints) → catalog load + strategy from `tail_signature` and args assembly for core.
- Startup validation: duplicate **`slug`** in **`[[melody_root]]`**; unknown **type** in `tail_signature` or unsupported (`command_id`, `tail_signature`) — explicit dev error; user overlay — logged row rejection. Same for **unknown `wire_class`**, **`kind`** vs `tail_signature` conflict, empty **`between_slots_any_of`** when `[[tail_wire_class]]` is used. With **`[alias_type]`** — conflict with actual **`[[parametric]]`** or **`command_id`** mismatch between **`[aliases]`** and **`[[parametric]]`**.
- Optional complexity: loader for **external grammar** via **`grammar_ref`** (§3.5); plugins as extension boundary — [§3.6](#adr0109-p-plugins).

---

## 5. Adoption steps (guide)

1. Add **loader DTOs**: internal snapshot as **`[[melody_root]]`-like** list (even if the file is still **`[aliases]` + `[[parametric]]`** from [§3.2.2](#adr0109-toml-migration)); optional **`[[tail_wire_class]]`**. Extend [`intent-melody-aliases.toml`](../../../IntentMelody/intent-melody-aliases.toml) (second file + merge only when truly needed, see §3.1).
2. Implement **`ParametricMelodyCatalog`** / shared **melody root catalog** (read-only after startup), no slug duplication across layers after normalization.
3. Convert [`intent-melody-aliases.toml`](../../../IntentMelody/intent-melody-aliases.toml) to **`[[melody_root]]`** ([§3.2.1](#adr0109-melody-root)) — **immediately by hand** or after a short [§3.2.2](#adr0109-toml-migration) phase; remove duplicate **`[aliases]`** / **`[[parametric]]`** for the same slugs.
4. Gradually replace branches in **`IdeCommandPaletteFilterOrchestrator`** and **`CascadeChordIntentSession`** with catalog lookup + deterministic args by (`command_id`, `tail_signature`).
5. Remove duplicates: `PaletteOnlyAliases` as primary truth — **move to TOML**; in code — strategy from `tail_signature` and deterministic args for built-in commands.
6. Update [intent-melody-language-v1.md](../../intent-melody-language-v1.md): link this ADR and catalog fields (`[[melody_root]]` and migration shape).
7. As needed (§3.5): **`grammar_ref`** on a catalog row (**`[[melody_root]]`** or **`[[parametric]]`**) or external strategy registry + parser for external format (XML/JSON + schema). Separately when a plugin model exists — [§3.6](#adr0109-p-plugins).

### Implementation status (**Implemented** — steps 1–6 of §5; §3.5–3.6 out of scope)

Alignment with **adoption steps** (§5 below):

| Step | ADR content | Fact in code/data |
|-----|------------------|---------------------|
| 1 | Loader DTOs, migration TOML normalization | `IntentMelodyAliases` → `IntentMelodyBundleState` / `IntentMelodyCatalogSnapshot` + overlay merge |
| 2 | Read-only root catalog | `IntentMelodyCatalog` + snapshot from bundle |
| 3 | Target **`[[melody_root]]`** in bundle | `IntentMelody/intent-melody-aliases.toml`: `melody_catalog_schema_version`, only **`[[melody_root]]`** and **`[[tail_wire_class]]`** (no `[aliases]` / `[[parametric]]`) |
| 4 | Palette and chord via catalog | `IdeCommandPaletteFilterOrchestrator`, `CascadeChordIntentSession`, `MelodyPaletteLineCommandPaletteExtensions` + `ParametricIntentMelody` use `IntentMelodyCatalog` |
| 5 | Remove `PaletteOnlyAliases` as truth | “Palette hint only” list in TOML (`show_usage_hint_if_bare_slug`); code keeps helper **`IsPaletteOnlyAlias`** (reads catalog), not a static slug list |
| 6 | Update `intent-melody-language-v1.md` | Links to ADR 0109 and **`[[melody_root]]`** / `[[tail_wire_class]]` fields — in [intent-melody-language-v1.md](../../intent-melody-language-v1.md) |

**Step 7** (`grammar_ref`, external parse strategies) and **§3.6** (plugins with their own binders) were **optional / next era** in the ADR — not “unfinished core”, but extensions after complexity grows.

Behavior and test details:

- **Load and validation:** `IntentMelodyAliases.Build` parses **`[[tail_wire_class]]`** and **`[[melody_root]]`**, checks **`wire_class`**, **`chord_commit`**, **`kind`** vs **`tail_signature`** (`IntentMelodyTailSemantics`).
- **Runtime catalog:** read-only `IntentMelodyCatalogSnapshot` + `IntentMelodyCatalog`; parametric parse uses wire classes (**`SingleRemainder`** / **`DelimitedSlots`**) from `ParametricIntentMelody`.
- **`chord_commit`:** `ParametricIntentMelody.ChordDefersInstantExecuteFor*` (**`enter`** and similar defer; **`immediate` / `instant`** — do not).
- **`palette_hint_slug`:** `ParametricIntentMelody.ResolvePaletteHintKey`; in bundle (e.g. **`wai`** → **`wai-url`**).
- **Tests:** `IntentMelodyCatalogWireTests`, `ParametricIntentMelodyTests`, `IntentMelodyAliasesTests`.

---

## 6. Link to MCP and command registry

This ADR **does not** change the `ide_execute_command` contract: still **`command_id` + JSON args**. The change is only **how args are produced from a melody string** and **how UX is aligned** across surfaces. Per-command argument schema may still be duplicated in **command documentation** / codegen [0018](0018-ide-commands-canonical-xml-documentation.md) (if adopted for IDE), orthogonal to the catalog (**`[[melody_root]]`** / migration **`[[parametric]]`**).
