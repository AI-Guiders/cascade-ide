# Agent Contributions

Stable feature-level registry of agent-delivered work that should remain visible in the repo history.

## Entries

### 2026-04-19

- Feature: Agent chat surface refresh for ADR 0031.
- Essence: introduced a first-class chat intent/snapshot pipeline (`ThreadNode`, `MessageNode`, `ConfirmationNode`, `DecisionEdge`), moved the product chat render to the Skia surface, and connected structured clarification batches to real chat/MCP flow.
- Agent / model: Cursor coding agent, GPT-5.4.
- ADRs: [0031](docs/adr/0031-agent-chat-clarification-batches-and-threading.md), [0057](docs/adr/0057-chat-surface-pipeline-adoption.md).
