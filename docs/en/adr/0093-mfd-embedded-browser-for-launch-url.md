<!-- English translation of adr/0093-mfd-embedded-browser-for-launch-url.md. Canonical Russian: ../../adr/0093-mfd-embedded-browser-for-launch-url.md -->

# ADR 0093: Built-in launch URL viewing on MFD (extension to profiles and launchBrowser)

**Status:** Accepted · Implemented  
**Date:** 2026-04-24

## Related ADRs

| ADR | Role |
|-----|------|
| [0090](0090-launch-profiles-and-debug-startup-configurations.md) | semantics of `launchBrowser` and URL from Kestrel / `ASPNETCORE_URLS` |
| [0002](0002-debug-human-agent-parity.md) | one meaning of launch - different *surfaces* of viewing, without a second “type” of debugging |
| [0075](0075-ui-topic-index-and-mfd-page-conventions.md) | MFD pages/slots |
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | cockpit: stack, log, hypotheses + session content nearby |
| [0091](0091-pfd-debug-situational-deck-hypothesis.md) | PFD vs Mfd |

---
## Context

[0090](0090-launch-profiles-and-debug-startup-configurations.md) fixed Kestrel profiles, import from `Properties/launchSettings.json` and the semantic flag **open browser** after the process starts. In the baseline implementation, this is an **external** opening of the URL (system browser), which coincides with the usual behavior of Visual Studio and does not require embedding the rendering engine.

At the same time, the CIDE product line is **cockpit** (MFD, debugging, agent): it is beneficial for the developer and agent **not to leave the IDE** in order to **see the rendered page** via the same `http(s)://…` that Kestrel gives - next to the stack, breakpoints and debug channel. The question is not about replacing MSBuild/DAP, but about the **third axis** after "which profile / which URL": **where** to show the same URL.

<a id="adr0093-decision"></a>

## Solution (direction)

1. **Do not replace** external browser: it remains the canonical default for `launchBrowser` (OAuth, extensions, familiar DevTools, isolated profiles). Built-in viewing is an **add-on** that is explicitly enabled.
2. **Option UX** (name of the key in the workspace or user settings - details during implementation), for example meanings: **external** | **MFD** | **ask once** / per-session. The launch and env semantics do not change: only the **navigation surface** changes after a successful `LaunchAsync`.
3. **URL source** is the same as for the current `KestrelLaunchBrowser` / `launchBrowser` logic: the first suitable `http`/`https` from the effective environment (including `ASPNETCORE_URLS` after the profile merge). Parity of “what was discovered” with the external regime is mandatory.
4. **Placement:** separate **page or MFD region** (see [0075](0075-ui-topic-index-and-mfd-page-conventions.md)), in a typical scenario next to the `DebugStack` / instrumentation, without intercepting the entire PFD zone (cf. [0091](0091-pfd-debug-situational-deck-hypothesis.md)).
5. **Embedding technology** - move to a separate sub-item of the implementation: on Windows - a natural candidate **WebView2**; cross-platform matrix (Linux/macOS) - a separate section (does not block the Proposed solution, but affects the roadmap).
6. **Agent Parity (MCP):** The `debug_launch`/profiles contract **doesn't** require knowledge of MFD. The agent still operates workspace + target + profile; choice **external vs MFD** - setting up the IDE for a person (and optionally an idempotent hint in `settings`, if ever needed in the protocol - only as an optional extension).

<a id="adr0093-consequences"></a>

## Consequences

- There is a **dependence** on the built-in web runtime and security policies (navigation, mixed content, localhost TLS) - the maintenance budget is separate from “just Process.Start”.
- We need **lifecycle** rules: changing the URL when restarting debugging, closing the tab when `debug_stop`, no “frozen” web view without a session.
- User Guide documentation: when to prefer MFD, when an external browser (in short, the product layer).

<a id="adr0093-non-goals"></a>

## Rejected / out of scope

- **Only** built-in browser without external path - rejected: breaks typical login scenarios and the usual DevTools circuit.
- Full **parity** with Chrome DevTools inside the IDE - outside the scope. For v1, the built-in mode requires **regular page rendering** using the same URL (as in an external browser: markup, styles, scripts on the page), **without** the Network, Application, Performance panel and the rest of the DevTools outline.
- **Removal** `launchBrowser: false` when selecting MFD - incorrect: the flag remains about “whether the URL should be opened at all”; surface - second level.

<a id="adr0093-implementation-status"></a>

## Implementation status
- Not implemented. Current baseline: external browser by [0090](0090-launch-profiles-and-debug-startup-configurations.md). True ADR captures direction and boundaries so as not to be confused with DAP/profile refactorings.

<a id="adr0093-implementation-checklist"></a>

## Implementation checklist (when we hire you)

1. Set up **surface** (external/mfd/ask) and route from the same path that now leads to `KestrelLaunchBrowser`.
2. MFD: page + web host (WebView2, etc.); agree with [0075](0075-ui-topic-index-and-mfd-page-conventions.md).
3. Debug life cycle events: start, stop, profile change - update or clear view.
4. Tests: if possible, headless/integration tests at the “URL sent to view” level, without a full E2E browser in CI (check in the pipeline).
5. User-facing: one mini-section in the User Guide, link from [0090](0090-launch-profiles-and-debug-startup-configurations.md) / this ADR.