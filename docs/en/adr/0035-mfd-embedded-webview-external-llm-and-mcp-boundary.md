# ADR 0035: Embedded browser in MFD, external web LLMs, and MCP boundary

**Status:** Proposed (intent and trust invariants; WebView and UX details — per roadmap).  
**Date:** 2026-04-11

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | MFD as secondary anchor; embedded browser as “navigation” layer |
| [0016](0016-agent-client-protocol-external-agent.md) | external agent / bridges to other environments |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | MCP in IDE — contract and process boundaries |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | multi-window, `MfdHostWindow` / secondary surfaces |
| [0020](0020-agent-reasoning-visibility-and-provider-limits.md) | agent reasoning visibility layers; honest provider limits |

---
## Context

In [0021](0021-pfd-mfd-cockpit-attention-model.md) the **MFD** zone is already described as a place for **secondary** tools, including an **embedded browser** — to avoid pulling gaze out of the “cockpit” into an external window unnecessarily. In practice it is also convenient to host **third-party LLM web UIs** there (e.g. search-engine AI mode): quick cross-check, documentation, “second opinion” beside code.

At the same time the IDE keeps an **MCP** loop ([0008](0008-mcp-contracts-and-testable-infrastructure.md)) and built-in/external **agent** ([0016](0016-agent-client-protocol-external-agent.md)): access to tools, files, processes — on the **native host** with an explicit trust model. A **web page** inside the embedded browser lives in the provider origin **sandbox**: it does not have the same API as the Cascade process and **no** direct connection to MCP servers on the user machine.

Without explicit boundaries it is easy to mix expectations: “web assistant in MFD sees my file / repo like the agent in Cursor”. That **does not** follow from placing a tab in the IDE and **must not** be implied by default.

## Solution

<a id="adr0035-p1"></a>

1. **Embedded browser in MFD — intentional surface.** Implementation targets **WebView2** (or platform equivalent) **inside** the MFD region / secondary window per [0017](0017-multi-window-workspace-and-agent-surfaces.md) and preset ([0010](0010-ui-modes-toml-configuration.md)): not a fourth semantic anchor in the sense of [0021](0021-pfd-mfd-cockpit-attention-model.md), but a **secondary** tool in the MFD zone. Bookmarks, start URL (including direct link to web LLM mode) — product configuration, not this ADR.

<a id="adr0035-p2"></a>

2. **Trust invariant: web ≠ native MCP client.** Content of an **arbitrary** HTTPS origin in the embedded WebView **does not** automatically get access to **MCP**, workspace files, secrets from `ai-keys.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)), or other IDE process privileges. This matches browser security and removes the attack class “page in MFD = elevated rights”.

<a id="adr0035-p3"></a>

3. **Forward / editor context to external web LLM — explicit only.** Any transfer of file text, code fragment, path, or workspace metadata from IDE to a third-party web service is a **deliberate user action** (selection, copy, future “share…” with confirmation) and/or separate product policy accounting for repo privacy. **No** implicit sync “like built-in agent with MCP”.

<a id="adr0035-p4"></a>

4. **Hybrid workflow (canon for v1).** Joining “web second opinion” and “native agent with tools” goes through the **human operator** and simple mechanisms: clipboard, explicit paste, links, if needed bridges per [0016](0016-agent-client-protocol-external-agent.md) (external client, deep link), without mandatory automation.

<a id="adr0035-p5"></a>

5. **“Friend web with MCP” — separate architectural line.** Options like browser extension, local gateway with user consent, API tunnel — **not** part of this ADR baseline; they need their own threat analysis (XSS, origin spoofing, token leaks), consent model, and if needed a **separate ADR** or appendix to [0008](0008-mcp-contracts-and-testable-infrastructure.md). Until such a decision, the product **does not** promise MCP access to a web page in MFD. Concrete direction “embedded web portal + Host Object → `IdeCommands`” — [0108](0108-web-ai-portal-host-object-tools-bridge.md) (**Accepted**).

## Consequences

- MFD UX may include a **predictable** layer “documentation and external LLMs in one cockpit” without mixing native agent privileges.
- Documentation and onboarding must **clearly** distinguish: *built-in agent / MCP* vs *third-party web UI in WebView*.
- Any future **web ↔ local tools** integration is evaluated separately and does not break §2–3 invariants without a new decision.

## Rejected / out of scope

- **Full unification** of “web LLM in MFD” and “IDE agent with MCP” into one indistinguishable entity — rejected as violating trust boundary and complicating security without separate design.
- **WebView certification** or a specific cloud provider policy — out of this ADR.
