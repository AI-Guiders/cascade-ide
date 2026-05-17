<!-- English translation of adr/0045-agent-chat-persistence-event-log-and-projections.md. Canonical Russian: ../../adr/0045-agent-chat-persistence-event-log-and-projections.md -->

# ADR 0045: Persistence of chat history - append-only events + projections

**Status:** Accepted · Implemented  
**Date:** 2026-04-14

## Related ADRs

| ADR | Role |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | model of refinement packages |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | model is primary, UI is secondary |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | transport does not equal history storage |

---
## Context

Chat in CIDE ceases to be a “one-time field” and becomes a work surface with:

- messages and streaming;
- clarification packages (`ClarificationBatch`);
- potential branches/threads;
- connections with tools and artifacts.

For such a form you need persistence, which:

1. not tied to a specific UI;
2. experiences the evolution of the scheme;
3. restored deterministically after a restart.

## Solution (direction)

1. Storage canon - **append-only event log** in NDJSON (`*.events.ndjson`), one event per line.
2. Session metadata - separate `*.meta.json` (id, created/updated, title, version).
3. The UI works through **projections** (in-memory now; index/search can be added later) rather than directly through "message files".
4. The payload of the event is stored as a JSON object (a JSON string in the model) with `schema_version` to expand the fields without migrating the entire history.
5. Attachments and heavy data - in separate files; store only the link/identifier in the event.

## Format v1 (minimum)

Directory: `workspace/.cascade-ide/chat-sessions/`

- `session-<id>.events.ndjson`
- `session-<id>.meta.json`

Event types v1:

- `message_added`
- `message_stream_delta`
- `message_completed`
- `clarification_batch_opened`
- `clarification_answer_submitted`
- `message_edited` - compensating event: new text for existing `message_id` in payload (`message_added` / `message_completed` are not overwritten).

The `message_added` and `message_completed` v1 payload includes the stable `message_id` (string without hyphens), `role`, `content`.

Export for agent: readable Markdown from the current projection (command `chat_export_readable`, optional entry to `chat-sessions/exports/`).

## Why not “immediately SQLite as a source of truth”

- In the early phase of the product, event log is easier to debug, diff and migrate.
- Projections can be changed without rewriting the canon.
- SQLite index can be added as an accelerator without breaking the truth format.

## Consequences

- A single history recovery layer appears for the person and the agent.
- Payload versioning discipline appears.
- We need retention/archive (size and age policy) as a separate step.

## Open questions

- Cleanup/archive policy (`N` days, `M` MB, manual pin).
- Policy for editing old messages (compensating event vs hard rewrite).
- Minimum set of editing secrets (redaction event).