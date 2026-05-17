<!-- English translation of adr/0081-parametric-intent-melodies-editor-line-ranges.md. Canonical Russian: ../../adr/0081-parametric-intent-melodies-editor-line-ranges.md -->

# ADR 0081: Parametric Intent Melody - Editor Line Ranges (`:start:end`)

**Status:** Accepted · Implemented  
**Date:** 2026-04-20  
## Related ADRs

| ADR | Role |
|-----|------|
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Command Melody, CascadeChord, keyboard-first |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id` and UI layers |
| [0013](0013-command-surface-and-discoverability.md) | palette and discoverability |
| [0070](0070-command-palette-direct-overlay-surface.md) | palette as overlay |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS - input overlay composition |
| [0072](0072-chat-topic-cards-intent-melody-keyboard-contract.md) | intent-first logins in the chat domain |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | attention zones / MFD |
| [0039](0039-workspace-navigation-affordances.md) | Workspace navigation - multiple views and "current file + related" |
| [0080](0080-intercom-naming-and-multi-party-channel-model.md#adr0080-future-modalities) | anchors to code - a related topic, does not duplicate this ADR |
| [0110](0110-roslyn-refactor-intent-melody-bridge.md) | Roslyn Range Refactorings - Intent Melody/IDE Bridge and Roslyn MCP |

## Summary

- Parametric **Intent Melody:** suffix `:startLine:endLine` for operations on text.
- Range validation; bridge to Roslyn - [0110](0110-roslyn-refactor-intent-melody-bridge.md).


### Outside ADR

| Document | Role |
|----------|------|
| [intent-melody-language-v1.md](../../intent-melody-language-v1.md) | IML v1: `c:` grammar and registry alias |
| [`IntentMelody/intent-melody-aliases.toml`](../../IntentMelody/intent-melody-aliases.toml) | overlay alias → `command_id` |
| [Roslyn MCP](../../../roslyn-mcp/README.md) | Roslyn MCP |

---
## Context

In IML v1, the tail after `c:` is a **mnemonic** that resolves to **`command_id`** via the registry ([`intent-melody-aliases.toml`](../../IntentMelody/intent-melody-aliases.toml), [ADR 0060 §11](0060-keyboard-chord-stack-fms-tactical-strategic.md#adr0060-p11)). For commands like “Git status” or “open Chat page” this is enough: the context is set by the focus and the current solution.

Operations on **active editor text** (select line range, delete range, **Extract Method** and other Roslyn range refactorings) fundamentally require a **parameter** - at least **line range** - otherwise the same mnemonic is ambiguous or is forced to rely only on the current selection, which is worse for “know the line numbers” scenarios.

---

## Problem

1. **Intent → command gap:** without parameters, the user is forced to first select the text with the mouse/keyboard, then call the command; for power-user with numbers from the log/diff this is slower than **one line of input** with a range.
2. **Surface parity:** if the option is only available through a menu or a separate dialog, the principle of **same `command_id` and same inputs** as palette and Melody ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) suffers, [0008](0008-mcp-contracts-and-testable-infrastructure.md)).
3. **Input errors:** out-of-file range or `start > end` should not have a **silent** destructive effect or cause you to edit the wrong part.

---

## Solution (suggested)

<a id="adr0081-p1"></a>

### 1. String range suffix

After a **registered** short alias (melody root without `c:` in registry terms) an **optional** suffix of the form **`:startLine:endLine`** is allowed:

- **`startLine`**, **`endLine`** — integers, **1-based**, **inclusive** (consistent with line numbers in the editor and typical compiler messages).
- Field separator - **`:`**; the parser, after matching the base alias **first**, resolves the prefix into a `command_id`/domain operation, then, if the tail continues with the pattern `:digits:digits`, interprets the pair as a range of strings.

**Illustrative examples** (final mnemonics and binding to `command_id` - a registry and product decision, not hardcoded into the parser):

| Input (fragment after `c:`) | Intent (landmark) |
|--------------------------|---------------------|
| `els:5:15` | Editor → Line → Select lines 5–15 in the active document |
| `eld:5:15` | Editor → Line → Delete in the specified range |

Canon of this ADR thread:
- For operations on a range of lines, use the **Editor → Line** (`el*`) model, without legacy/synonyms `es*`.
- Family of melodies **Refactor → …** by range (`rmx`, `rix`, …) and a combination with Roslyn / **Roslyn MCP** (`roslyn_get_code_actions` / `roslyn_apply_code_action`) **are not included in the required minimum of this ADR** - see. [0110](0110-roslyn-refactor-intent-melody-bridge.md).

Expansion of **IML v1** in [`intent-melody-language-v1.md`](../../intent-melody-language-v1.md) (new sub-clause about the parametric tail) - **in a separate step** after stabilizing the format; Until then, this ADR is a **direction standard** for implementation and documentation.

<a id="adr0081-p2"></a>

### 2. Validation and refusal

- The range **must** fit within the current length of the document (in lines); in case of violation - **explicit feedback** (palette status line, short annunciator in IDS - according to [0079](0079-ide-display-system-ids-overlay-pipeline.md)), **without** using a destructive action “as it happens”.
- Condition **`startLine <= endLine`**; otherwise, the same error class as out-of-bounds.

<a id="adr0081-p3"></a>

### 3. Roslyn refactorings (Extract Method and analogues)

Commands that in the IDE already expect **selection** in the editor (including Roslyn: `roslyn_get_code_actions` with a range for Extract method and analogues) should behave predictably:

- If there is a **non-empty selection** in the active document, **selection priority** is possible over the numeric suffix (product rule: “an explicit gesture wins”) **or** an unambiguous policy “the suffix always sets the range” - **fix during implementation** and reflect in help.
- If there is no selection - **synthetic selection** along the lines `startLine`–`endLine` (whole lines, including line breaks according to the rule accepted in the editor) before calling the same path as from the UI.

<a id="adr0081-p4"></a>

### 4. UX: discoverability and input mode

- **Indicator of recognized melody:** with active **command mode** / input in the palette, show a brief analysis: basic alias + numeric range (and if an error occurs, the reason). The goal is to reduce “I thought I entered something else” errors without requiring a long log.
- **Chord vs palette only:** An open question is whether to allow a parametric tail in **CascadeChord** (`Ctrl+K` → sequence of letters) or limit it to **palette** for the sake of fewer typos without previews. The solution is separate; this ADR allows both options as implementations.

---

## Consequences

- Palette/Melody string parser extension and VM binding: editor commands that accept a range receive a **single** contract from IML, not just from pointer.
- The registry [`intent-melody-aliases.toml`](../../IntentMelody/intent-melody-aliases.toml) can reference the **same** `command_id` as without parameters; semantics “with/without parameter” - in the command handler or thin adapter.
- MCP Documentation / `IdeCommands`: for the corresponding commands, **args** or a parallel path “from palette with range” are specified if necessary.

---

## Rejected alternatives

- **MCP with explicit arguments only** - breaks keyboard-first and the single intent line in the human IDE.
- **Only space as delimiter** (`5 15`) - conflicts with the IML v1 direction "spaces inside the tail do not cut tokens" ([§3.4](../../intent-melody-language-v1.md#34-spaces)); suffixing with **`:`** after a full match alias is more reproducible.
- **Binding to mouse/gutter only** - does not cancel, but does not replace parametric input for users without a pointer on the line.

---

## Open questions

1. Limit parametric shapes to **palette only** on the first release or immediately allow chord input.
2. Are **columns** or **subranges inside a row** needed in the next iteration? Move beyond the minimum `:start:end` across rows.
3. Synchronization with future **anchors to code** from [0080](0080-intercom-naming-and-multi-party-channel-model.md) (deep link): general “range in document” layer or individual contracts.