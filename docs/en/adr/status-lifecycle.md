# ADR statuses: lifecycle (for humans)

**Purpose:** a single agreement on **how to read and update** the “Status” field in ADRs — without listing individual record numbers (full index — [README.md](README.md)).

**On the documentation site:** status-grouped navigator (auto-generated from ADR headers) — [../site/adr-nav/index.md](../site/adr-nav/index.md) · public: [ai-guiders.github.io/cascade-ide/en/site/adr-nav/](https://ai-guiders.github.io/cascade-ide/en/site/adr-nav/).

---

## Why two levels

- **First tag** answers: *is the decision adopted or replaced?*
- **Second tag** (optional) answers: *are code/delivery obligations for this ADR already met in the product?*

This separates **architectural decision** from **implementation fact**.

---

## First tag (required)

| Value | Meaning |
|--------|---------|
| **Proposed** | Draft for discussion: not yet adopted as direction; may change or be rejected. |
| **Accepted** | Decision adopted: norm for code and review until **Superseded** / **Deprecated**. |
| **Superseded** | Replaced by another ADR; text must link explicitly (“instead use …”). |
| **Deprecated** | Obsolete; do not use for new work (historical reference). |

Clarifications in parentheses are allowed: **Accepted (direction)**, **Accepted (strangler)** — when it matters not to confuse with “everything is already in code”.

---

## Second tag: **Implemented**

Written after **Accepted** with **` · `** (space, middle dot, space):

**`Accepted · Implemented`**

**When to use:** main delivery for the ADR is **merged in code** (and contract tests/docs if needed); behavior is **current canon**. In the ADR header, briefly state *what* was done (one line), without duplicating the whole diff.

**When not to use:** decision is adopted but implementation is intentionally phased / strangler / partial scope — keep **Accepted** with a note in parentheses (**partial**, **by plan**, **MCP implemented**, etc.) until “done” criteria are agreed.

**Partial implementation** without the `Implemented` second tag: e.g. **`Accepted (partial)`** or a dedicated “Implementation state” subsection in the body.

---

## What to update when status changes

1. ADR file header (`**Status:**`).
2. Row in [README.md](README.md) table.
3. If needed — matching row in [architecture-policy.md](../../architecture-policy.md) (topic navigator).
4. Short line in the “Versioning this navigator” section of `architecture-policy.md` when policy or a large index changes.

One logical change — **one logical commit** (as usual in the repo).

---

## Relation to agents and CI

ADR statuses are **not** a substitute for tracker tasks; they record **norm**. CI checks and MCP/CLI contracts are described in the ADRs themselves or linked docs (e.g. [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md)).

---

## Brief for assistants (IDE / Cursor)

- First tag: **Proposed** / **Accepted** / **Superseded** / **Deprecated** — see table above.
- Mark code delivery with **`Accepted · Implemented`** (via ` · `); in the ADR header — briefly *what* was done.
- Do not add **`Implemented`** when scope is intentionally partial — keep **Accepted** with a note (“partial”, strangler, separate implementation subsection).
- On status change, sync: ADR header, [README.md](README.md) row, and [architecture-policy.md](../../architecture-policy.md) if needed.

The repository ignores **`.cursor/`**; you may duplicate these points in a **local** Cursor rule on your machine if you want.
