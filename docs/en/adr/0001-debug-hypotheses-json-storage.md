<!-- English translation of adr/0001-debug-hypotheses-json-storage.md. Canonical Russian: ../../adr/0001-debug-hypotheses-json-storage.md -->

# ADR 0001: Storing debugging hypotheses in a single JSON file

**Status:** Accepted  
**Date:** 2026-04-02

## Related ADRs

| ADR | Role |
|-----|------|
| [0003](0003-debug-ui-mode-separate-from-power.md) | **"Hypotheses"** tab in the lower Debug panel (not the chat column). |

---
## Context

We need a persistent representation of the list of **hypotheses** (text, status, identifiers) for a debugging scenario consistent with a Cursor-like workflow: hypotheses can be supplemented from the IDE and, if necessary, from the MCP. Streaming formats and human-readable documents were considered.

## Solution

Use **one JSON file** per open workspace, with **schema version root field** and an array of hypothesis records, for example:

- root: `{ "version": 1, "hypotheses": [ ... ] }`;
- each record has, among other things, a **status** field; schema migrations - through the `version` increment.

**Statuses (meaning for UI and text):** in the product language “**passed**” (the hypothesis has been formulated, there is no verdict yet) - this is the same as in the data **`open`** (“open”). There is no need for a separate fourth state “out”: it does not duplicate `open`. The other two verdicts are **not confirmed** / **confirmed** (in the code the enum can be called, for example, `rejected` and `confirmed`; the exact names are at implementation, the main thing is three semantic slots).

Typical location (like other IDE artifacts): **`.cascade-ide/debug-hypotheses.json`** in the workspace root (the exact path is fixed in the code/doc of the feature).

**Where to look at the list in the UI:** in Debug mode - **Hypotheses tab** in the **bottom panel** (next to “Debugging”); details and a fallback option - section **"Hypotheses: UI zone"** in [0003](0003-debug-ui-mode-separate-from-power.md).

## Consequences

- Simple loading/saving as a whole, updating a record by `id` without line-by-line search in the log.
- If you save frequently, the entire file is overwritten - for the expected number of hypotheses (tens to hundreds) this is acceptable.
- Git diffs are readable as changes in one JSON; If desired, you can later add a separate append-only event log without changing the main storage.

## Rejected alternatives

- **NDJSON / JSON Lines** as the **main** storage of the current list - rejected: changing the status of the same hypothesis complicates the model (line rewriting or index) with the same UI with the list; the format remains appropriate for an **optional** event log, not a single source of truth for the state.
- **Markdown/sections** - deferred: worse for strict machine model and automatic updates from MCP without markdown agreement.