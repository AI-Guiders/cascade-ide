<!-- English translation of 0060-keyboard-chord-stack-fms-tactical-strategic.md. Canonical Russian: ../../adr/0060-keyboard-chord-stack-fms-tactical-strategic.md -->

# ADR 0060: Chord layer (FMS-style), S/T, and overlay — keyboard-first extension (ADR 0013)

**Status:** Accepted (partial: Intent Melody catalog + chord/palette v1; full FMS T/S overlay — per ADR)  
**Date:** 2026-04-19  

## Related ADRs

| ADR | Role |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | palette, toolbar, discoverability — **this ADR does not replace** the palette |
| [0070](0070-command-palette-direct-overlay-surface.md) | palette as overlay surface |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP and commands |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `IdeCommands`, `hotkeys.toml`, registry |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | multi-window, focus |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD / MFD |
| [0032](0032-hud-banner-configuration-and-grammar.md) | HUD / mode captions — optional |
| [0055](0055-skia-instrument-composition-pipeline.md) | Skia — overlay |
| [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) | tactical / EFB on MFD, Manager |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | chat topic navigation — **chat-domain refinement**, not a replacement for this ADR |
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | parametric melody `:start:end` for editor; builds on Command Melody from §11 |

### Outside ADR

| Document | Role |
|----------|------|
| [north-star — keyboard-first](../../design/north-star-cursor-mcp-cascade-workbench-v1.md) | north-star — keyboard-first |
| [chord-notation-cascadeide.md](../chord-notation-cascadeide.md) | chord notation cascadeide |

**Partial refinement (chat-domain):** navigation across **chat topics** (topic cards, drill-in/back, minimal intent command set, parity across palette / Melody / Chords) is fixed in [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md). This ADR (**0060**) remains the canon for the general **Melody**, **CascadeChord**, and **Command Melody (`c:`)** model; **0072** does **not** cancel §1–§11 wholesale and does **not** reassign S/T/M/E for the cockpit outside chat-topic navigation.

---
## Context

[0013](0013-command-surface-and-discoverability.md) establishes the **command palette** as the anchor (string search, full catalog) and **keyboard-first** as a product direction. For the cockpit that is not enough: an FMS pilot first picks a **section**, then a **parameter** — two meaningful steps without typing. CascadeIDE also needs a **thinking scale** (tactical vs strategic, see [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md)) that **does not reduce** to “open another window”.

A **second keyboard entry** is needed: a **chord layer** (prefix → second key), aligned with `IdeCommands` ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)) and not duplicating the palette.

## Summary

- Chord layer (**Ctrl+K**), FMS-style tactical/strategic, overlay on top of [0013](0013-command-surface-and-discoverability.md).
- Extends discoverability without replacing the palette and IdeCommands.
- Tied to intent melody and chat-domain ([0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md)).

---

## Decision

<a id="adr0060-p1"></a>

### 1. Two entries, one command model

| Entry | Purpose | Anchor in [0013](0013-command-surface-and-discoverability.md) |
|------|------------|------------------------------------------------------------------|
| **Command palette** (default hotkey **Ctrl+Q** in shipped `hotkeys.toml`) | Full catalog, search by name, recents | Decision §2 item 2 |
| **Chord root** — working name **`CascadeChord`** (default hotkey **Ctrl+K** in the product; override — user `hotkeys.toml`) | Short FMS flow: prefix → second key → execute **one** command from a pre-limited set of “cockpit” actions | keyboard-first extension **without** canceling the palette |
| **Same `command_id` as text in the palette** — prefix **`c:`** (**Command Melody**, [§11](#adr0060-p11)) | Compact mnemonic (`c: gs`, …) without changing window; fuzzy on title/id as before | Not a third “physical” entry: still **Ctrl+Q**, extended by query string |

**Rule:** **Ctrl+K** does not replace **Ctrl+Q**; the user chooses “search by name”, “go by known chord letter”, or enter **melody** after opening the palette. Conflicts with editor/OS hotkeys — detect on load and reflect in UX (see open questions).

<a id="adr0060-p2"></a>

### 2. First level after `CascadeChord`: **S / T** (scale canon)

The **canonical** first choice after the prefix is **thinking scale** and depth of coupling with Roslyn MCP ([0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md)), not “which monitor”:

| Key (after prefix) | Name | Meaning |
|---------------------------|-----|--------|
| **T** | **Tactical** | Local semantics around the cursor; tactical PFD / Forward contour; narrow contract |
| **S** | **Strategic** | Global / layered graph; **MFD / EFB** contour; `Profile.GlobalMap` in the sense of [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md) |

**Plus:** one axis reflects CascadeIDE’s essence — **we change depth of immersion**, not only “view”.

Executing a specific command (e.g. `cascade.focus_tactical_map`, `cascade.focus_strategic_efb`) — via **`IdeCommands`** and MCP parity ([0008](0008-mcp-contracts-and-testable-infrastructure.md), [0002](0002-debug-human-agent-parity.md)).

<a id="adr0060-p3"></a>

### 3. Alternative / second level: **M / E** (displays)

Splitting **MFD** vs **EFB** ([0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md): EFB = **MFD**, not PFD) may move to a **second chord step** or a **parallel** letter set when the product needs explicit **display** binding:

| Key | Name | Meaning (idea) |
|---------|-----|----------------|
| **M** | MFD focus | Instruments, logs, terminal — “on the ground” / diagnostics |
| **E** | EFB / strategic | Map, architecture on MFD |

**Mixing rule:** do not put S/T and M/E on **one level** without a documented scheme (e.g. prefix → **S or T** → optional second chord **M or E** to refine surface). Minimal v0 — **S/T only** after prefix.

<a id="adr0060-p4"></a>

### 4. “Vector” variant (combined)

Optional (separate iteration or hotkey preset):

- **Digits 1 / 2 / 3** after prefix — quick **focus on monitor / TopLevel** ([0017](0017-multi-window-workspace-and-agent-surfaces.md)) in user configuration order.
- **F** — toggle **Flight** / exit “flight mode” (tie to [0010](0010-ui-modes-toml-configuration.md) — do not mix with S/T without explicit UI label).

This is **not** mandatory for v0; recorded so independent chord roots are not spawned without a registry.

<a id="adr0060-p5"></a>

### 5. Chord state machine

- After **CascadeChord** the IDE enters **“waiting for second key”** for a short **timeout** (default and override — optionally in `settings.toml`, see §7).
- **Esc** — exit without action; focus returns per [0013](0013-command-surface-and-discoverability.md) / [0017](0017-multi-window-workspace-and-agent-surfaces.md).
- **Editor:** do not steal text input while chord-wait is active unless focus policy explicitly gives events to the application window (implementation details in code; tests for “do not eat a letter in the line”).

<a id="adr0060-p6"></a>

### 6. Overlay hints (discoverability)

After pressing the prefix, show **semi-transparent hints** (letters/labels) over **zones** or in a **screen corner** for a short time — so the user does not guess “what next”. Implementation:

- preferably **Skia** overlay on the host ([0055](0055-skia-instrument-composition-pipeline.md): overlay must not become a separate “instrument” duplicating the pipeline);
- **Reduced motion** / disable hints — settings flag;
- do not obscure critical PFD indicators without density policy.

<a id="adr0060-p7"></a>

### 7. Mode indication on PFD (situational awareness)

A small line such as **`MODE: TACTICAL`** / **`STRATEGIC`** / sub-modes **ECHELON** / **COMBAT** ([0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md)) in a PFD corner or HUD layer ([0032](0032-hud-banner-configuration-and-grammar.md)) — **recommended** so visuals match what the profile Manager does. Single source of truth (VM / CDS / mode service), not duplicated out of sync with the chord.

<a id="adr0060-p8"></a>

### 8. Configuration: where things live

| What | Where |
|-----|-----|
| Binding **prefix** and chords to **`command_id`** | **`Hotkeys/hotkeys.toml`** (ship) + `%LocalAppData%\CascadeIDE\hotkeys.toml` ([0013](0013-command-surface-and-discoverability.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)) |
| Execution implementation | **`IdeCommands`** + input handler (chord state machine) in C# |
| Optional: `chord_timeout_ms`, `chord_overlay_enabled` | **`settings.toml`** or UI-adjacent section — **do not** duplicate the full key map in `settings.toml` ([0013](0013-command-surface-and-discoverability.md) — rejected variant: store all hotkeys only in settings) |

**Work order (recommendation):** first **command ids and chord machine** in C#; then lines in **`hotkeys.toml`**; then overlay and MODE line.

<a id="adr0060-p9"></a>

### 9. Command migration logic and step evolution (implementation)

**What we do not move to chord without separate discussion**

- **Editor / OS standard** gestures users expect “everywhere”: save (**Ctrl+S**), undo/redo (**Ctrl+Z** / **Ctrl+Y**), cut/copy/paste, often **Ctrl+W** / close tab, **Ctrl+F** in editor. Keep on direct gestures or editor settings; do not force into **CascadeChord** for uniformity alone.
- **Already fixed global IDE hotkeys** from `hotkeys.toml` (**Ctrl+Q** palette, **F5** debug, …) are not broken: chord is a **second** entry, not a replacement.

**Target layered scheme (after v0)**

1. **CascadeChord** (prefix, e.g. **Ctrl+K**).
2. **First letter step — S / T scale** (§2): tactical vs strategic; tie to Roslyn MCP profile / attention contour ([0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md)).
3. **Second step — subsystem** (letter “pages”): e.g. **M** = intent map (**P** / **F** / **D**), **B** = build/tests, **G** = Git, etc. So we do not mix a dozen unrelated letters and thinking scale on one level.

**Current multi-step scheme (code)**

- **Prefix** → **M** → **M / P / F**: cockpit zones — **MFD** (expand region), **PFD** (expand region), **Forward** (focus editor). “Zone settings/focus” metaphor without a second **Ctrl+M** root: second letter is **M** right after prefix (FMS page), third — zone choice.
- **Prefix** → **S** → **P / F / D**: intent map — view / level / detail (as in [0055](0055-skia-instrument-composition-pipeline.md)).
- Overlay shows allowed keys **for the current step**; timeout and **Esc** reset the machine (§5).

**Discoverability rule**

- Any command available only via chord must have the **same `command_id`** in palette and MCP ([0008](0008-mcp-contracts-and-testable-infrastructure.md)), so “forgot the letter” ≠ “lost the action”.

<a id="adr0060-p10"></a>

### 10. UX philosophy: invisible instrument, muscle memory, rehearsal vs performance

> “A good actor is one you do not see. Because only the character remains.”

**IDE as invisible actor.** The product must not “perform itself”: endlessly showcasing tools, hotkeys, menus, panels, overlays. The interface should **vanish** where possible and leave **code, task, developer thought** at the center. CascadeIDE cockpit is the **invisible actor** metaphor: PFD, MFD, Forward, intent map are not “IDE face for IDE’s sake” but **pilot instruments**, extensions of thought. In work the pilot does not think “I press a button on MFD” — they hold **course** while hands find the gesture.

**Chords and muscle memory.** After enough practice the user stops reciting key sequences and operates **semantic layers** (e.g. Modification → Navigation → View → Strategic as intent mnemonic, not abstract key list). Fingers lock the pattern; mind stays on the task. Concrete **step grammar** (one root prefix vs extra modifiers, letter order, S/T axis vs intent-first G/B/D/… families) evolves with registry and hotkeys; the philosophical layer is **not tied** to one letter table but requires any grammar version to stay **readable** and **teachable**.

**Metaphors (one direction — background and shedding the “extra”):**

| Metaphor | For CascadeIDE |
|----------|----------------------------|
| Pärt tintinnabuli | Two voices: contextual background (prefixes/input layers) + “melody” of following letters — simple, strict, meditative structure |
| Satie furniture music | IDE as **background**: does not demand constant attention, holds work atmosphere |
| Good actor | IDE **vanishes**; code and task remain |

**Consequences for ADR and notation**

- **Documentation notation** must be **transparent**: the reader should not spend mental energy decoding. Notation like `<C-k> …` in [chord-notation-cascadeide.md](../chord-notation-cascadeide.md) is supplemented with **mnemonic gloss** where it speeds understanding (what intent, not only which VK).
- **Overlay (§6)** — **prompter**, not director: appears when the next step must be hinted and **does not block the scene** (code, critical indicators). See density policy and Reduced motion in §6.
- **Palette (Ctrl+Q) vs chord (CascadeChord)** — **rehearsal vs performance**: palette — full catalog and learning; chord — fast entry for mastered actions ([0013](0013-command-surface-and-discoverability.md), §1 of this ADR). Both use one **`command_id`** model.

**Tie to intent-first and surface routing.** The idea “chord fixes **intent** (Git, Build, Debug…), **where** to show result is surface routing” **aligns** with this section: cockpit surfaces remain display context, not mandatory first level of mental formula for every gesture. v1 grammar detail (command families, S/T axes) — separate ADR/registry iterations; §10 goal — **UX principles**, not a concrete letter table.

**See also:** product narrative “invisible instrument” vs cloud-assistant risk class — [cascadeide-philosophy-v1.md](../../design/cascadeide-philosophy-v1.md), [ADR 0071](0071-ai-assistance-sovereignty-locality-invisibility.md).

<a id="adr0060-p11"></a>

### 11. Command Melody (`c:`) — palette, mnemonic alias, and bridge to chord

**Language spec (lexicon, EBNF, motivation, open questions):** [intent-melody-language-v1.md](../intent-melody-language-v1.md) (**Intent-based Melody Language**, IML v1).

**Origin:** melody (`c:`) spec was shaped in the **Comet** line (external dialogue); here — product norm. Pointer in personal KB canon (agent-notes) — `knowledge/personal/assistantLines/comet/comet-command-melody-cascade-palette-2026-04-19.md`.

**Purpose.** Prefix **`c:`** introduces **melody** mode in the **command palette** ([0013](0013-command-surface-and-discoverability.md), [0070](0070-command-palette-direct-overlay-surface.md)): short **mnemonic aliases** in a developer-first vocabulary. The user can type compact forms like `c: gs`, `c: br`, `c: da` and get the command without relying only on fuzzy search on long titles. **Frequency → length** heuristic and deferred disambiguation — [intent-melody-language-v1.md](../intent-melody-language-v1.md) §3.5.

**Goal.** The mode is **not** to replace ordinary search but **fast semantic entry**, shared across:

- palette (query string);
- chord layer (**CascadeChord**);
- canonical **`command_id`** in registry ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md));
- MCP / agent call ([0008](0008-mcp-contracts-and-testable-infrastructure.md)) — same identifier.

**Three representations of one command**

| Layer | Example |
|------|--------|
| Human title | `Git: Status` |
| Melody alias | `c: gs` |
| Canonical id | `git.status` *(or equivalent in `IdeCommands`; exact string — registry [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md))* |

The palette **must** find and show the command by **any** of these (title, alias, id).

**Syntax**

- Common namespace prefix: **`c:`** — enter melody.
- After `c:` one or more mnemonic tokens; **v1 base form** — **two letters**: first = **family**, second = **action**.
- Examples (logical names; actual `command_id` — per registry):

| Input | Purpose (meaning) |
|------|---------------------|
| `c: gs` | Git → Status |
| `c: gc` | Git → Commit |
| `c: gp` | Git → Push |
| `c: gm` | Git → Merge |
| `c: gsu` | Git → Submodules |
| `c: br` | Build → Run / Rebuild (per registry terminology) |
| `c: bt` | Build → Test |
| `c: da` | Debug → Attach |
| `c: dc` | Debug → Continue |
| `c: dl` | Debug → Launch (`debug_launch`) |
| `c: cps` | Chat → **P**age → **S**how (`show_chat_page`; DOI) |
| `c: cs` | Chat → **S**end — send input draft as user (`send_chat` without `message`) |
| `c: cex` | Chat → **ex**port (readable Markdown) |
| `c: ctf` | Chat → **T**hread → **F**ork (three-letter DOI: domain → object → intent; `fork_chat_thread`) |
| `c: ers` | Environment → **R**eadiness → **S**how (`show_environment_readiness_page`) |
| `c: ts` | **T**erminal → **S**how (`show_terminal_panel`) |

Melody registry in repo: **`IntentMelody/intent-melody-aliases.toml`** (see [intent-melody-language-v1.md](../intent-melody-language-v1.md)).

**v1 families (narrow start, recognizable domains)**

| Letter | Family |
|-------|--------|
| **g** | Git |
| **b** | Build |
| **d** | Debug |
| **r** | Run |
| **e** | Editor |
| **m** | Map (intent map / map navigation) |
| **c** | Chat |
| **t** | Terminal (panel / terminal page in secondary contour) |

“Show page” transitions in the secondary contour are encoded **domain first** (intent): e.g. Chat → Page → Show = `c: cps`, not synthetic prefix **P** = Pages (**`pcs`**, **`pes`**, …) that puts IDE topology above command meaning. For **environment readiness** DOI fixes a chain like **Environment → Readiness → Show** (`ers` when alias appears in registry), not “Pages → Environment”.

This aligns with **intent-first**: the developer thinks **task domain**, not IDE topology ([§10](#adr0060-p10)).

**Alias selection principles**

1. Alias **recognizable** to developers without separate training.
2. Alias **shorter** than human title but not shell-golf “cipher”.
3. Alias **stable** even if UI wording shifts slightly.
4. Alias expresses **intent**, not **surface placement** (bad base example: tie to “primary deck” instead of command meaning).
5. **Frequency → length (heuristic):** the more typical the command for a workday, the shorter the reasonable alias in registry (Huffman-prefix analogy; [intent-melody-language-v1.md](../intent-melody-language-v1.md) §3.5). First — **base** by consensus and review; **re-sort by measured frequency** (incl. telemetry) — **deferred** until registry and palette stabilize.

**Conflict resolution**

1. Within one **workspace** one melody alias → exactly **one** canonical `command_id`.
2. On conflict: either **fix** one mapping in registry, others as secondary matches; or require **three-letter** form; or mark conflicting alias **invalid** until resolved.
3. **Human title** and **canonical id** remain **always** valid search paths even if alias is ambiguous.

**Palette behavior in melody mode**

- On `c:` the palette switches to melody mode: empty tail after `c:` — **help** and **top families**; on `c: g` — available Git commands and tails; on `c: gs` — direct match (e.g. Git: Status).
- Result line shows **human title**, **alias**, and optionally **chord hint**. Example:  
  `Git: Status — c: gs — git.status`

**Link to chord layer (one semantic path)**

Melody **does not live apart** from chord: the same path as `c: gs` in the palette, in intent-first mental model, is chord **`Ctrl+K` → `G` → `S`** (family and action letters), and in registry — the same **`command_id`**. Palette gives **text** compact form; chord — **keyboard**; MCP — **identifier**.

**UX constraints**

- `c:` — **accelerator**, not the only way: command must also be found by **ordinary fuzzy** on human title.
- In UI show alias **next to command** (and chord hint if needed) so the language is learned gradually.
- Overlay / help may suggest: “Try `c: gs` for Git Status” ([§6](#adr0060-p6)).

**Minimal v1 dictionary (starter set)**

| Alias | Logical meaning (canonical id — confirm in registry) |
|-------|------------------------------------------------------|
| `c: gs` | git.status |
| `c: gc` | git.commit |
| `c: gp` | git.push |
| `c: gm` | git.merge |
| `c: gsu` | git.submodules |
| `c: bt` | build.test |
| `c: br` | build.run or build.rebuild |
| `c: da` | debug.attach |
| `c: dl` | `debug_launch` |
| `c: dc` | debug.continue |
| `c: ms` | map.semantic / show semantic map (per adopted naming) |

Dictionary extension — as commands appear in `IdeCommands`; duplicates and conflicts — per rules above.

---

## Consequences

- A **second UX pattern** appears beside the palette; user docs must explain **Q vs K**.
- Command registry ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)) gains **melody alias** layer and conflict rules; palette ([0070](0070-command-palette-direct-overlay-surface.md)) — **`c:`** mode and title / alias / id triple display.
- Tests: timeout, Esc, hotkey conflict, MCP `ide_execute_command` for same `command_id`.
- Dependency on [0059](0059-roslyn-mcp-profiles-manager-tactical-strategic-efb.md): tactical/strategic must align semantically with chord commands.

---

## Open questions

- Is **Ctrl+K** reserved for the editor (Vim/Emacs-like plugins in future)?
- One global chord machine per app or account for **second TopLevel** ([0017](0017-multi-window-workspace-and-agent-surfaces.md))?
- Overlay letter localization (EN-only in v0?)?
- How **S/T axis** (§2) and **intent-first G/B/D/…** ([§11](#adr0060-p11)) combine in one state machine: two chord presets, mode switch, or sequential migration?

---

## Rejected at ADR level

- **Replace palette with Ctrl+K** — rejected: different jobs (catalog vs short FMS flow).
