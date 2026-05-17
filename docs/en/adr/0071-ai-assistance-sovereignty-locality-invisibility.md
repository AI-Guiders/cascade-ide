<!-- English translation of adr/0071-ai-assistance-sovereignty-locality-invisibility.md. Canonical Russian: ../../adr/0071-ai-assistance-sovereignty-locality-invisibility.md -->

# ADR 0071: Principles of AI/assistant integration in IDE - sovereignty, locality, invisibility

**Status:** Proposed  
**Date:** 2026-04-19  
## Related ADRs

| ADR | Role |
|-----|------|
| [0013](0013-command-surface-and-discoverability.md) | palette, discoverability |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP, parity |
| [0020](0020-agent-reasoning-visibility-and-provider-limits.md) | visibility of the agent's reasoning |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | settings and secrets |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | providers façade |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | chords; §10 - UX philosophy |

### Outside ADR

| Document | Role |
|----------|------|
| [cascadeide-philosophy-v1.md](../../design/cascadeide-philosophy-v1.md) | cascadeide philosophy v1 |

---
## Context

IDE user experience has historically relied on **predictability** and **control**: action → expected effect, local operation, clear boundaries. The spread of **cloud inline assistants** in the built-in editor has created a risk class: assistance becomes **more noticeable than the task**, **sovereignty** is weakened (disabling, hint policy), **cognitive load** increases (“what will the model do next?”).

CascadeIDE is inherently focused on an **agent-first** loop with **observability** (MCP, ADR, command parity), rather than an opaque "model within each character" mix without consensus and without an alternative path.

---

## Solution

<a id="adr0071-p1"></a>

### 1. Fix the principles of integration of any AI/assistance function

| Principle | Requirement |
|---------|-----------|
| **Sovereignty** | The user can **disable or limit** the class of hints/automation at the product level (not “just hide visually” without disabling the contract with the cloud, if there is such a contract - this must be **explicit** and controlled). |
| **Locality and borders** | Preferred scenarios where the **source of truth** and critical path are **repository, MCP, IDE**; external providers - **with an explicit** boundary and policy ([0008](0008-mcp-contracts-and-testable-infrastructure.md), [0028](0028-user-settings-toml-localappdata-and-secrets.md)). |
| **Transparency** | Behaviors that affect the code and work loop are **documented** (ADR, user help) rather than remaining just in the vendor model. |
| **Invisible by default** | UX features that compete with code for attention pass the “prompter, not director” bar** ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) §6, §10). |
| **One command model** | Any action available to the assistant or automation has a path through **`command_id`** and parity with the palette/MCP, where applicable ([0013](0013-command-surface-and-discoverability.md), [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)). |
| **Honesty of reasoning** | Visibility layers and provider limits - according to [0020](0020-agent-reasoning-visibility-and-provider-limits.md); do not imitate “full thinking” without designated boundaries. |

<a id="adr0071-p2"></a>

### 2. Anti-pattern (not target UX)

Consider the following profile as an **unwanted reference** for baseline CascadeIDE:

- cloud inline code completion **by default everywhere**, without permanent shutdown;
- behavior that **makes it** difficult to predict what will end up in the editor's buffer;
- mixing **subscription/vendor account** with the basic cycle “open solution - edit - build - debug” without a clear alternative.

Details and historical narrative - in [cascadeide-philosophy-v1.md](../../design/cascadeide-philosophy-v1.md); ADR captures the **product norm**, not an overview of competitors.

<a id="adr0071-p3"></a>

### 3. Non-purposes of this ADR

- Do not prohibit **optional** integration of external LLMs ([0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md)) - with an explicit trust boundary.
- Do not lock in **specific SKU** or license of third-party products (policies change).
- Do not replace the **technical** specification of the agent facade ([0038](0038-agent-facade-ai-provider-and-tool-orchestration.md)) - only product principles of UX and trust.

---

## Consequences
- New features with AI/assistant are tested for compliance with §1; in case of conflict - a separate discussion or a conscious exception with an entry in the ADR.
- User documentation may refer to [cascadeide-philosophy-v1.md](../../design/cascadeide-philosophy-v1.md) as a **human-readable** layer; the standard for implementation is this ADR and related ADRs via links.

---

## Open questions

- “sufficient off” criteria for specific inline-assist classes (full off vs depth modes).
- Is a separate **compliance matrix** needed for corporate modes (air-gapped, without cloud).