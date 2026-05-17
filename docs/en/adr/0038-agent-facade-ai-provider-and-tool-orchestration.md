<!-- English translation of adr/0038-agent-facade-ai-provider-and-tool-orchestration.md. Canonical Russian: ../../adr/0038-agent-facade-ai-provider-and-tool-orchestration.md -->

#ADR 0038: Agent Facade - LLM Providers, Chat and Tool Orchestration

**Status:** Accepted · Implemented (current code); section “Direction” - draft ideas, not obligations  
**Date:** 2026-04-11  
## Related ADRs

| ADR | Role |
|-----|------|
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts and testability |
| [0016](0016-agent-client-protocol-external-agent.md) | External ACP Agent |
| [0020](0020-agent-reasoning-visibility-and-provider-limits.md) | visibility reasoning and provider limitations |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | evolution of chat UI |

### Outside ADR

| Document | Role |
|----------|------|
| [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) | canon `IdeCommands` |

---
## Context

The product simultaneously requires: **interactive chat** with the model, **separate external agent circuit** (Cursor CLI over ACP) and **experimental offline mode**, where the model selects IDE tools and optionally external MCPs. The development of the agent branch **laged** behind the work on the cockpit, attention zones (PFD/MFD/CDS) and command infrastructure - while the code already captures useful boundaries that are worth describing so as not to lose direction.

## Solution (as now)

<a id="adr0038-p1"></a>

### 1. Single point of chat streaming - `AiProviderManager`

`Services/AiProviderManager.cs` - **facade for text chat**: accepts the provider key, the `ChatMessage` history, optionally the path and text of the current file, the context minimization flag. Delegates the choice of provider implementation and model name via **`AiProviderResolver`** (in `MainWindowViewModel.ResolveProvider`: Ollama, Anthropic, OpenAI-compatible, DeepSeek; for `CursorACP` the chat provider is **not** created - see point 3).

When context minimization is enabled, a block from **`ContextMinimizer`** (diagnostics and signatures for `.cs`) is mixed in for the current file, then **`IAiChatProvider.StreamChatAsync`** is called.

<a id="adr0038-p2"></a>

### 2. Provider contract - text streaming only

`IAiChatProvider` specifies the **minimum** contract: `IAsyncEnumerable<string>` over a list of messages. There is **no separate **native tool-calling** protocol at the provider level in the code; the autonomous mode is built on top of this contract (clause 4).

<a id="adr0038-p3"></a>

### 3. Three user inputs to the “agent” in a broad sense

| Contour | Where | Transport |
|--------|-----|-----------|
| Chat (LLM from Settings) | `Features/Chat/ChatPanelViewModel` | `AiProviderManager.StreamChatAsync`, except for the Cursor ACP branch |
| Cursor ACP | The same `ChatPanelViewModel`, if the active provider is `CursorACP` | `CursorAcpChatConnection` (stdio to external agent), **not** via `AiProviderManager` |
| Autonomous agent | `Features/AutonomousAgent/AutonomousAgentService` | The same `AiProviderManager` for **receiving the raw response of the model** by prompt; JSON parsing and tool calls - in the service |

ACP is orthogonal to the built-in LLM façade; this is consistent with [0016](0016-agent-client-protocol-external-agent.md).

<a id="adr0038-p4"></a>

### 4. Autonomous loop: “model → JSON → execution”

`AutonomousAgentService` implements a **simple cycle of steps**: a prompt with a purpose, security level and history; the model should return **JSON only** (`type`: `tool_call` or `final`). Parsing - extracting the first JSON object from the response (`ExtractFirstJsonObject`), without a full function-calling API on the provider side - is directly noted in the XML doc of the class.

- **`scope: "ide"`** - call `IIdeMcpActions.ExecuteCommandAsync` with `ide_command_id` and arguments; corresponds to the canon `IdeCommands` / [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md).
- **`scope: "external"`** — call **`McpClientService.CallToolAsync`** by the key `prefix.toolName`; allowed only at level **L3**; at L1/L2, external MCPs are blocked with an explanation in the route.

<a id="adr0038-p5"></a>

### 5. Security and offline confirmations

Levels **L1 / L2 / L3** limit the set of valid IDE commands (L1 - no high-risk edits and git commit/push, etc.). For some commands at L2, **`RequestConfirmationAsync`** is possible before execution; external MCPs - L3 only (item 4).

<a id="adr0038-p6"></a>

### 6. External MCPs as process clients
`McpClientService` connects servers via **stdio**, indexes tools as **`{ToolPrefix}.{tool.Name}`**, sends the list to a standalone prompt and executes `CallToolAsync`. This is a **client** to external processes; separately from the “IDE itself acts as an MCP server” scenario for an external agent ([0008](0008-mcp-contracts-and-testable-infrastructure.md), MCP protocol in the repository).

## Consequences

- **Pros:** few abstractions; one streaming path for chat and for raw response offline; explicit separation of IDE commands and external MCPs by `scope` and by L3 level.
- **Cons/technical debt:** fragile JSON parsing from arbitrary model text; there is no single **orchestrator** “plan → tools → observations” at the domain level; the list of allowed `ide_command_id` in the prompt is **wired with a string** in the `BuildPrompt`, and not generated from the command directory; `CursorACP` does not participate in the offline loop.

## Direction (ideas so you don't get lost)

These items are **not** accepted as obligations; priority is given by the roadmap and cockpit/attention focus.

1. **Single orchestration layer** (conditional name: Agent Orchestrator): remove from `AutonomousAgentService` and from chat the general rules of “one step”, logging, token limits, tool policy - so as not to create discrepancies between modes.
2. **Explicit tool-calling contract** where the provider supports it (OpenAI/Anthropic, etc.): gradually reduce the share of “JSON in free text”; for Ollama, leave a fallback or a separate “structured output” path with schema validation.
3. **Ollama as a transport**, not as a semantic “agent type”: distinguish between **local model** (via Ollama HTTP/API) and **policy** (what is allowed to be called); avoid mixing “Ollama = whole agent” in UX and settings.
4. **ID command directory for prompt** - generate from the same source of truth as the MCP doc (`IdeMcpToolCatalog`/registry) so that offline mode keeps up with new `ide_*`.
5. **Linking with [0031](0031-agent-chat-clarification-batches-and-threading.md):** structured clarification batches and offline mode can share **the same “dialogue step” format** (at the data level), even if the UI is different.
6. **Observability ([0020](0020-agent-reasoning-visibility-and-provider-limits.md)):** explicit layers “response to user / step traces / raw provider log” for offline mode - according to the same principles as for chat.
7. **Tests without network:** contract tests of decision parser and routing `scope` with dummy `IIdeMcpActions` and `McpClientService` - strengthen [0008](0008-mcp-contracts-and-testable-infrastructure.md).

## Rejected or deferred alternatives (briefly)

- **Only external agent (ACP) for everything** - rejected as the only way: you need built-in chat and autonomy without the required Cursor CLI.
- **One protocol for chat and MCP** - premature: chat remains text; MCP - for tools and individual scripts ([0016](0016-agent-client-protocol-external-agent.md)).