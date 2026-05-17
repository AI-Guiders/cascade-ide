<!-- English translation of adr/0083-ai-mode-and-nested-settings-toml.md. Canonical Russian: ../../adr/0083-ai-mode-and-nested-settings-toml.md -->

# ADR 0083: `settings.toml` — `ai.mode` discriminant and nested sections (local / acp / mcp_only / cloud)

**Status:** Accepted · Implemented  
**Date:** 2026-04-20

## Related ADRs

| ADR | Role |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | path and secrets |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | provider facade and tools |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | ACP, MCP in session |
| [0016](0016-agent-client-protocol-external-agent.md) | ACP as external agent |
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | parametric Intent Melody `:start:end` for editor — adjacent, not part of `ai.mode` |

## Summary

- **`[ai]`** section in `settings.toml`: `mode` = local | acp | mcp_only | cloud.
- Nested sections; **no** backward compatibility with legacy `provider`.

---

<a id="adr0083-context"></a>

## Context

The **`[ai]`** section in user TOML currently mixes several independent axes:

- **Which contour answers assistant text** (local Ollama, cloud API, Cursor ACP).
- **Flag “do not call built-in LLM, wait only for MCP”** (`chat_mcp_only`) — by meaning a **separate interaction mode**, not “another provider.”

Readability of file and docs suffers: users must hold `provider` + boolean combinations in head.

---

<a id="adr0083-decision"></a>

## Decision

<a id="adr0083-mode-discriminant"></a>

### 1. Top-level discriminant

In **`[ai]`** introduce mandatory (for canon) field:

| TOML key | Meaning |
|----------|---------|
| `mode` | One of: **`local`**, **`acp`**, **`mcp_only`**, **`cloud`**. |

**Why not bare `mcp`:** collides with **protocol and connected MCP servers** (tools in ACP session, external stdio servers, etc.). Chat mode value should read as **assistant reply source**, not “MCP enabled.” Canonical mode name — **`mcp_only`**: built-in LLM is **not** called after user send; assistant text comes **only** from external MCP contour (e.g. `send_chat` with assistant role). Same semantics as `assistant_via_mcp` or `no_builtin_provider`; TOML picks short **`mcp_only`**.

Semantics:

| Value | Purpose |
|-------|---------|
| **`local`** | Built-in local provider: **`[ai.local]`** with **`backend = "ollama"`** (and later values) plus **`[ai.local.ollama]`** for model/endpoint. Other local backends — separate `backend` and `[ai.local.<name>]`. |
| **`acp`** | Chat via **Cursor Agent / ACP** (`cursor-agent`): paths, `model_id`, MCP server **injection** policy in ACP subsection; **not** same as `mcp_only` mode. |
| **`mcp_only`** | **MCP-only reply** for assistant text: built-in provider (Ollama/cloud) **not** used in this sense. Connected MCP servers as tools may exist in `local` / `acp` / `cloud` — orthogonal. |
| **`cloud`** | Cloud providers with API keys (Anthropic, OpenAI-compatible, DeepSeek, etc.): provider choice and credentials in nested tables. |

<a id="adr0083-nested-tables"></a>

### 2. Nested tables (direction)

Keys and exact nesting fixed at implementation; normative **skeleton**:

```toml
[ai]
mode = "local" # local | acp | mcp_only | cloud

[ai.local]
backend = "ollama"

[ai.local.ollama]
# model, base_url, request_timeout

[ai.acp]
# cursor_acp_path, cursor_acp_model_id, acp_auto_inject_ide_mcp

[ai.mcp_only]
# limits/flags specific to “no built-in LLM, reply only from MCP”

[ai.cloud]
# active_provider = "anthropic" | "openai" | "deepseek"

[ai.cloud.anthropic]
[ai.cloud.openai]
[ai.cloud.deepseek]
```

Pluggable cloud provider catalog (dynamic `[[...]]` arrays, etc.) **out of scope**; v1 — fixed nested tables above.

**Naming:** TOML **`snake_case`**, aligned with `CascadeTomlSerializer` / [0028](0028-user-settings-toml-localappdata-and-secrets.md). Nesting **`[ai.local.ollama]`** / **`[ai.cloud.*]`** gives context: leaf keys **`model`**, **`base_url`** without long prefixes. For local **service/API** choice in **`[ai.local]`** canonical field — **`backend`**; do not use synonym **`engine`**.

**Local mode: “type” vs model.** Weight format type (GGUF, SafeTensors, …) **not** in user **`settings.toml`** — not IDE contract. Two levels:

1. **`backend`** in **`[ai.local]`** — string discriminant: **`ollama`**, later **`openai_compatible`**, etc. Must **match** active **`[ai.local.<backend>]`** table.
2. **`model`** — **API model id** for chosen engine (e.g. `qwen2.5-coder:7b` for Ollama), not file format type.

**Beyond Ollama (product, not necessarily v1):** same abstract **OpenAI-compatible HTTP** on localhost — LM Studio, llama.cpp OpenAI API mode, vLLM, LocalAI — e.g. **`[ai.local.openai_compatible]`** with `model`, `base_url`, optional key ref in `ai-keys.toml`.

<a id="adr0083-secrets"></a>

### 3. Secrets

Still **do not** store API keys in `settings.toml`: separate **`ai-keys.toml`** ([0028](0028-user-settings-toml-localappdata-and-secrets.md)). This ADR only defines **which fields** reference keys by name/slot.

---

<a id="adr0083-backcompat"></a>

## Backward compatibility

**Not required.** Legacy keys `provider`, `chat_mcp_only`, flat `default_ollama_model`, cloud URLs at `[ai]` root **removed** from canon: migration — manual file edit and sample updates (`docs/samples/settings*.toml`). Optional one-time script/doc “how to rewrite old file” outside mandatory runtime automigration.

---

<a id="adr0083-consequences"></a>

## Consequences

1. **`AiSettings` and related VM** restructured for discriminant + nested types; Tomlyn serialization must reproduce nested tables predictably.
2. **AI settings UI** maps to `mode` and subsections instead of flat `Ollama|…|CursorACP` list.
3. **Docs and samples** — single example with `[ai].mode` and nested blocks.
4. **Tests** — TOML deserialization contract for new shape; remove tests on old keys.

---

<a id="adr0083-rejected"></a>

## Rejected / deferred alternatives

- **Keep only `provider` + flags** — rejected as less readable for user and ADR.
- **Mode named `mcp`** — rejected: ambiguous with MCP protocol and connected servers; canon — **`mcp_only`**.
- **Automigration on load** — not done by requirement; reduces hidden surprises.
- **Plugins and extensible cloud provider registry** — deferred; v1 canon — fixed `[ai.cloud.anthropic]` / `openai` / `deepseek`.

---

<a id="adr0083-implementation"></a>

## Implementation status

Implemented in code (models, Tomlyn serialization, VM, “AI and chat” panel, samples). Extensions (e.g. `openai_compatible` in `[ai.local]`, cloud model editing from UI) as needed.
