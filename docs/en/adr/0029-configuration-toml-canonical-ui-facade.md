<!-- English translation of adr/0029-configuration-toml-canonical-ui-facade.md. Canonical Russian: ../../adr/0029-configuration-toml-canonical-ui-facade.md -->

# ADR 0029: Configuration — **TOML-First** (On-Disk Canon); **Holistic** Settings UI — **Deferred**; Point UI — **Canon Facade**, Not a Second Truth

**Status:** Accepted · Implemented (TOML-first on disk; holistic settings UI — deferred; point UI — facade)  
**Date:** 2026-04-08  
**Updated:** 2026-04-08 — holistic center deferred; subsection “Why point UI”; dynamic UI from model perspective; point UI = code weight. Details — [§ History](#adr0029-history).

## Related ADRs

| ADR | Role |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | Path and format of `settings.toml` |
| [0010](0010-ui-modes-toml-configuration.md) | Modes and `workspace.toml` merge |
| [0015](0015-editor-toml-syntax-highlighting.md) | TOML in built-in editor; external editor with rich tooling — conscious option |
| [0013](0013-command-surface-and-discoverability.md) | Palette, hotkeys in separate file |
| [0027](0027-small-team-focus-vs-public-maturity.md) | Axis B: standalone settings app and heavy onboarding — deferred backlog until triggers |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | Some keys in merged `workspace.toml` — team canon in repo |

## Summary

- **TOML-first** configuration; holistic settings UI — deferred.
- Point UI is a **facade** over the same file, not a second source of truth.

---

## Context

Three facts hold at once in the product:

1. **Files** are the accepted storage canon: user `settings.toml`, bundle/repo for modes and chrome ([0010](0010-ui-modes-toml-configuration.md), [0028](0028-user-settings-toml-localappdata-and-secrets.md)).
2. **UI** already changes some preferences (panel visibility, UI mode, providers, LSP, etc.) via model and save to disk.
3. Discussion oscillates between **“TOML only”** and **“full settings screens”** — without a dedicated ADR it is easy to mix expectations: what is “real” truth, must every key have UI, is living in one file alone allowed.

We need **one priority model**: disk and file format **define canon**; any UI that touches settings **must not** create a second truth — but **how much** screen UI (one button vs full “settings center”) is a separate product decision aligned with [0027](0027-small-team-focus-vs-public-maturity.md).

---

## Decision

### 1. Canon — **text configs on disk** (TOML where already adopted)

- User preferences — **`settings.toml`** and related files under `%LocalAppData%\CascadeIDE\` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).
- Layout/metrics/mode presets — **`UiModes/`** and merge with **`.cascade/workspace.toml`** ([0010](0010-ui-modes-toml-configuration.md)).
- **Secrets** — separate file **`ai-keys.toml`**, not in `settings.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)).

Extending keys — **model + file + serialization first**, then (as needed and on axis B) UI elements.

### 2. **Holistic** settings UI — **deferred** ([0027](0027-small-team-focus-vs-public-maturity.md))

**Holistic** means: **single page/center “all settings”**, separate **settings application**, large wizards — what [0027](0027-small-team-focus-vs-public-maturity.md) puts in deferred backlog until triggers (external contributor, RC, explicit pain, etc.).

- This is **not** a ban on **point** toggles in the main window that exist or appear with features.
- This is **not** a ban on documenting “edit `settings.toml`” as a primary path for part of the audience.

Net: **we do not invest now** in a full-screen discoverability settings product; **we do** live on TOML + docs + examples ([0027](0027-small-team-focus-vs-public-maturity.md)) until the queue says otherwise.

### 3. Point UI — when present, **facade of canon** (not a parallel database)

When a control in the app changes a preference stored in `settings.toml` (or other canonical file):

- it uses the **same** model (`CascadeIdeSettings`, etc.) as serialization;
- save goes through **`SettingsService.Save`** (and equivalents), **without** a shadow in-memory-only registry as source of truth;
- **no** policy “settings live only in UI and sometimes export” — see rejected alternatives.

**Facade** in this ADR = **architectural rule** for existing or point-added UI: it reflects the file, does not replace it with a hidden copy. It is **not** a promise to **collapse** all config into rich UI in the next release (see §2).

#### Why point UI if everything can be edited in TOML?

Canon remains the **file**; point UI **does not** add a second truth and is **not** required for every user.

- **Friction:** frequent actions (panel visibility, UI mode, etc.) are cheaper with a cockpit click than opening `%LocalAppData%\CascadeIDE\`, finding a snake_case key, saving, and sometimes restarting.
- **Rare one-off setup** is fine: e.g. enable external agent (ACP), set `cursor-agent` path — convenient from UI **or** the same key in `settings.toml` per docs; both entries hit one canon ([0016](0016-agent-client-protocol-external-agent.md)).
- “I never open settings UI, everything in TOML” is **valid and expected** for part of the audience; “I only opened UI once for one option” **does not** contradict TOML-first — alternative **entry**, not canceling the file.

### 4. **TOML-only** mode is first-class

- Editing `settings.toml` (and other canonical files) manually, in the **built-in** editor or **external** ([0015](0015-editor-toml-syntax-highlighting.md)), is a normal workflow including for agents and machine diffs.
- Missing **holistic** settings UI (§2) is **not** a gap by itself. Missing UI for individual keys is also OK; a checkbox per model field is **not** mandatory ([0027](0027-small-team-focus-vs-public-maturity.md)).

### 5. When to add/extend **point** UI on top of TOML

- High **frequency**, **safety** (secret masking), **live validation** — natural **point** control candidates (still §3 rules).
- Complex structures (MCP server JSON lists, etc.) — **as** UI if they reduce hand-editing errors; serialized result still lives in the canonical file field.
- **Holistic** “settings center” — only after [0027](0027-small-team-focus-vs-public-maturity.md) triggers or explicit pain, not default queue.

### 6. Session limitation (implementation honesty)

- **`SettingsService.Load()`** typically runs at app start; **direct** edits to `settings.toml` on disk during a session **may** need restart for all subsystems to pick up values (unless a key gets explicit reload). This is **not** an argument against TOML-first; “reload settings” or a watcher is a **separate** task, not mixed with canon question.

---

## Consequences

- New settings: design **key in model and file** first; **holistic** UI — not required and not default queue; point UI — as needed.
- Docs for users and agents: “how to change X” may start with **file path and key**; screens are optional convenience and alternate entry (§3, “Why point UI”).
- [0027](0027-small-team-focus-vs-public-maturity.md): deferred **settings app/center** aligns with this ADR: on-disk canon exists, rich UI later per triggers.
- Point UI **increases code volume** (bindings, VM, tests) — add consciously; alternative to form sprawl when a holistic center appears — see “Perspective” below.

---

## Perspective (not a implementation commitment)

When a **holistic** settings center is no longer deferred per [0027](0027-small-team-focus-vs-public-maturity.md), consider **not** a pile of hand-built screens but a **dynamic form** built from the **same model** as `settings.toml` serialization (e.g. reflection/generator on `CascadeIdeSettings` + **metadata** layer).

**Plus:** new model/TOML field — with metadata, appears in UI **without** N manual bindings; one canon.

**Not “CLR type only”:** need labels, order, groups, hidden fields, localization; **secrets** stay outside this form ([0028](0028-user-settings-toml-localappdata-and-secrets.md)); **JSON blobs** and nonstandard fields — separate templates or multiline editor.

This is **not** “build now” and **does not** cancel §2 (holistic layer still not default queue); recorded as **future direction** if a trigger pulls settings center from backlog.

---

## Rejected alternatives

- **UI only, TOML as hidden export** — rejected: breaks transparency, diffs, agent workflow, “fixed in Notepad”.
- **Two sources of truth** (part registry, part TOML) without explicit canon — rejected: duplication and drift.
- **Mandatory UI for every `CascadeIdeSettings` field** — rejected: excessive for current axis B ([0027](0027-small-team-focus-vs-public-maturity.md)); TOML-only remains valid forever for advanced users.

---

## Change history

<a id="adr0029-history"></a>

| Date | Change |
|------|--------|
| 2026-04-08 | Holistic center deferred; subsection “Why point UI”; dynamic UI from model perspective; point UI = code weight. |
