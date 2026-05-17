<!-- English translation of adr/0087-microsoft-agent-framework-builtin-agent-orchestration.md. Canonical Russian: ../../adr/0087-microsoft-agent-framework-builtin-agent-orchestration.md -->

# ADR 0087: Microsoft Agent Framework (MAF) - a guide to the **embedded** agent framework orchestration layer

**Status:** Accepted · **Next step: PoC**  
**Date:** 2026-04-22

## Related ADRs

| ADR | Role |
|-----|------|
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | LLM façade, standalone JSON loop, `McpClientService` |
| [0083](0083-ai-mode-and-nested-settings-toml.md) | `ai.mode`, incl. `mcp_only` |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP contracts |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | ACP, MCP tools in session |
| [0082](0082-acp-ide-mcp-loopback-single-process.md) | MCP loopback in one process |
**Summary:** the **Agent-first** reference point in the product is transferred to the stack: **MAF** - the target layer for future orchestration of the built-in agent; **next step** - **PoC** (separate branch/project), without obligation to immediately merge into the main application.

---

<a id="adr0087-context"></a>

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | integration into `main` and update [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) - based on the results of the PoC |

## Summary

- **Microsoft Agent Framework** - Embedded agent orchestration guide.
- The next step is PoC; communication with [0104](0104-cognitive-decomposition-loop-for-maf-prompt-orchestration.md).

---
## Context

1. **Built-in** “agent track” (full-fledged framework: stable tool-calling, tests, step policy) **essentially not deployed**: there is a backlog in the code (`AutonomousAgentService`, minimal contract `IAiChatProvider`, external MCPs as a client), but **no** mature verification of scripts in the product.
2. [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) in the “Direction” section already mentions the idea of ​​a **single orchestrator** instead of the fragile “JSON in free text”.
3. [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) (MAF) - **open** (MIT) multi-language stack with **first-class .NET support**; The repository contains samples for Azure OpenAI, OpenAI, Foundry, **Ollama**, etc.

User question: **does it make sense to specifically consider MAF** when starting work on agent-based piping, and **does this affect MCP modes**.

---

<a id="adr0087-pros-cons"></a>

## Pros / Cons (pre-decision analysis)

<a id="adr0087-pros"></a>

### Pros (for relying on MAF for future inline loop)

| Plus | Explanation |
|------|-----------|
| **Less reinventing the wheel** | Ready-made agent abstractions, providers, samples for .NET; less native code for “loop: model → tool → observation”. |
| **Evolution instead of JSON-hack** | Reduces the priority of [0038 “Direction” p.2](0038-agent-facade-ai-provider-and-tool-orchestration.md#adr0038-p2) (extract JSON from the text) - provided that the selected provider/client in MAF closes the necessary calls. |
| **Multi-provider** | One stack for Azure / OpenAI-compatible / Ollama, etc.; easier to align with **cloud/local** in [0083](0083-ai-mode-and-nested-settings-toml.md). |
| **Documentation and Ecosystem** | Microsoft Learn, samples, predictable direction for .NET. |
| **Coincidence with the “early” stage** | While there is **no** volume of your own orchestrator poured into prod, **the cost of changing the approach is lower** than after years of investment in a custom engine. |

<a id="adr0087-cons"></a>

### Cons (risks and costs)

| Minus | Explanation |
|------|-----------|
| **Integration volume** | MAF is not a plugin for the “Submit” button: you need adapters for chat, offline mode, limits, routes, L1–L3; regression **local/cloud/ollama**. |
| **Moving Target** | Relatively young stack; **breaking** updates are possible - you need version pinning and a migration plan. |
| **Doesn't address "IDE specifics"** | The binding to `IIdeMcpActions`, the `IdeCommands` directory, and UI policies remains **ours**; MAF is a **model ↔ generic tools** layer, not a cockpit replacement. |
| **Duplicate with SK (if available)** | If we later bring in the Semantic Kernel, we need a **clear boundary** (both in SK and in MAF), otherwise there are two “centers of gravity”. |
| **Microsoft stack dependency** | Legally, MIT and the repo are open; **product** - binding to priorities and MS abstractions. |

### Benefit Summary
- **It is beneficial to consider MAF** if the goal is to **focus** the team on CIDE specifics (IDE commands, routes, UX), and **not** write a stable multi-provider agent-loop from scratch.
- **Less beneficial** as an "emergency replacement" if the next release is **mcp_only** / ACP without a built-in agent: MAF integration **doesn't** unlock the user in these modes any more than they already are (see below).

---

<a id="adr0087-decision"></a>

## Solution

1. **Accepted:** **MAF** - target stack for implementing the “single orchestration layer” item from [0038 “Direction” p.1](0038-agent-facade-ai-provider-and-tool-orchestration.md) for the **built-in** agent circuit; the "from scratch/Semantic Kernel only" alternative for this role is **not** prioritized until the PoC shows otherwise.
2. **Currently in production:** **PoC** in a separate branch or separate `samples`/console project within the repo (minimum: one provider, such as Ollama or OpenAI-compatible; one stub tool; `RunAsync` call / equivalent) - **without** the obligation to immediately merge into the main `CascadeIDE` UI.
3. **Criteria for transition to integration in `main`:** completed PoC + **list of adapters** to [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) (chat, autonomous, policy L1–L3) + **assessment** of lock-in risks; then **patch** [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) in the spirit of “Solution” with a link: what remains in `AiProviderManager`, what is in MAF; if necessary, a separate ADR for UI migration.

<a id="adr0087-tfm-packages"></a>

### TF Compatibility: CIDE and MAF packages

- **CIDE:** main TFM - **`net10.0`** (`CascadeIDE.csproj`, `CascadeIDE.Tests`, etc.); We **don’t do downgrades for the sake of MAF**.
- **NuGet (checked against package metadata):** [Microsoft.Agents.AI](https://www.nuget.org/packages/Microsoft.Agents.AI/) in the release line (e.g. **1.2.0**) explicitly includes target monikers **net10.0**, **net9.0**, **net8.0** (as well as .NET Standard 2.0 and .NET Framework 4.7.2 for other scenarios). Therefore, **`net10.0` CIDE is covered** by the core MAF library; no separate “cutting” at 8/9 is required.
- **MAF repository:** examples in `dotnet/samples` build and run scenes with `--framework net10.0`, sample prerequisites (eg Ollama) indicate **.NET 10 SDK or later** - in one vector with CIDE selection.
- **Transitive packages** (`Microsoft.Agents.AI.OpenAI`, Foundry, etc.): during PoC and major updates, check the **Frameworks / Dependencies** tab on NuGet **for the same version** that you pin: conflicts with `net10.0` are not expected in the kernel, but a separate provider could theoretically lag behind (rarely) - then **point** check `dotnet restore` / `dotnet build`.

<a id="adr0087-legacy-autonomous-path"></a>

### Parallel "old" autonomous path (feature flag)

**Solution:** separate flag / long dual-path for the current implementation of `AutonomousAgentService` (JSON from text) **do not** put in. **Why:** the built-in agent track **does not** have an external legacy set of users and **is not** brought to a mature contract; The “old” way is a design backlog, not a commitment to compatibility. When implementing MAF (or another orchestrator), it is enough to **replace the implementation** in a branch with regression based on scenarios and tests, without the required parallel option.

**We do not do the following until the agreed upon result of the PoC:** migrating the product code in the main application, changing dependencies in the root `CascadeIDE.csproj` without separate approval, changing the public MCP API.

---

<a id="adr0087-mcp-compatibility"></a>

## Impact on MCP (why `mcp_only` and "IDE as server" don't break)
| Topic | Meaning |
|-----------|--------|
| **MCP protocol, `ide_*` registry, external stdio servers** | Remains **infrastructure** IDE and contract for clients; MAF **does not** replace MCP and **does not** supersede [0008](0008-mcp-contracts-and-testable-infrastructure.md). |
| **`ai.mode = mcp_only`** ([0083](0083-ai-mode-and-nested-settings-toml.md)) | Built-in LLM **not called** for assistant text; the response comes from the **external** MCP circuit. It is **orthogonal** to MAF: MAF touches the **inner** “model in IDE process” loop. The `mcp_only` mode **doesn't** require MAF and **doesn't** get "worse" by not having MAF. |
| **ACP/Cursor-agent** | External agent by [0016](0016-agent-client-protocol-external-agent.md) **not** about MAF; mixing MCP into the ACP session - as in [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md). |
| **Standalone + `McpClientService`** | Today: model (via facade) + JSON + calling tools. **Future** connection with MAF: the logic of “who and how calls `CallToolAsync` / IDE commands” can **move** to the MAF orchestrator, but the **protocol** for calling external MCPs **remains** the same infrastructure layer. |

**In short:** switching **inline** script to MAF **doesn't** change the semantics of **MCP-only** and **doesn't** remove MCP from the IDE; the (hypothetically) implementation of the agent's **built-in** loop changes when it **comes** into development.

---

<a id="adr0087-consequences"></a>

## Consequences (direction taken)

- In the TECH documentation for the orchestration of the built-in agent, there is a **named** reference (**MAF**) instead of an undefined “custom orchestrator”.
- After PoC: dependencies `Microsoft.Agents.*` (and transitively - providers), update policy, CI for compatibility.
- [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) - **patch** based on the results of the PoC: the “Decision / Direction” section with a link that remains `AiProviderManager`, which goes to MAF.

---

<a id="adr0087-open-questions"></a>

## Open questions (clarify before/during PoC)

1. One **PoC provider** (Ollama vs cloud) and “success” criterion for tool-calling.
2. Contact [Semantic Kernel](https://github.com/microsoft/semantic-kernel) if scripts appear in the repo that overlap in area of ​​responsibility (avoid duplication of orchestration).

---

**Verified:** 2026-04-22 (status [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md), [0083](0083-ai-mode-and-nested-settings-toml.md); MAF public repository, MIT license in repo root; **compatibility TF:** [Microsoft.Agents.AI 1.2.0 on NuGet](https://www.nuget.org/packages/Microsoft.Agents.AI/1.2.0#supportedframeworks-body-tab) - **net10.0** in the list of included TFMs). **History:** 2026-04-22 - direction **accepted**, fixed **next step: PoC**.