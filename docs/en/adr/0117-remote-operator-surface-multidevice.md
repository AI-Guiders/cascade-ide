<!-- English translation of adr/0117-remote-operator-surface-multidevice.md. Canonical Russian: ../../adr/0117-remote-operator-surface-multidevice.md -->

# ADR 0117: Remote operator surface - multi-device operator (remote control, not mobile IDE)

**Status:** Proposed  
**Date:** 2026-05-16  
**Updated:** 2026-05-16 - remote surface client: **PWA** (canonical choice).

## Related ADRs

| ADR | Role |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | Multi-window on one station (**not** remote) |
| [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Web in MFD → `IdeCommands` (orthogonal) |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Web Trust Boundary ↔ MCP |
| [0016](0016-agent-client-protocol-external-agent.md) | External ACP Agent |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Clarifications Intercom vs PFD |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id`, confirmations |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | `IdeCommands`, MCP |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | Transport restoration |
| [0099](0099-ide-databus-typed-events-and-projections.md) | IDE Events → Projections |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | Chat/event history |

## Summary

- **Remote operator:** PWA remote control from a phone/other PC, Operator Gateway.
- Not mobile IDE; complements multi-window ([0017](0017-multi-window-workspace-and-agent-surfaces.md)).
- Communication with the web-portal bridge ([0108](0108-web-ai-portal-host-object-tools-bridge.md)).

**Outside the repo:** vision **agent-forge** (worst case scenario, API + browser) - personal canon `agent-notes`.

---

## 1. Context

**Powers:**

- The operator (person) works not only at **one** desktop: a second PC, tablet, phone - for **monitoring**, **replying to the agent**, **confirmations**, changing the WORK/HUMAN mode, without transferring the full IDE.
- **[ADR 0017](0017-multi-window-workspace-and-agent-surfaces.md)** solves **multi-monitor on one station** (`MainWindow`, `MfdHostWindow`, `presentation` presets). This is **not** a “left the office - continued from the phone” session.
- **[ADR 0108](0108-web-ai-portal-host-object-tools-bridge.md)** gives **built-in** web in MFD with a bridge to IDE for **external** web model. This is not an **operator console** from another device and is not a replacement for native Intercom.
- Line **Friction / environment-first:** convenience is measured by **worst path** (weak network, browser only, first time). If it is impossible to **see the status** and **make a decision** there, and heavy task closure is possible only behind the desktop, this is acceptable **only** if the desktop is clearly in the loop; otherwise the operator on the second device is “second class”.

**Problem:** without an explicit architecture it is easy to fall into (a) **mobile IDE** - unrealistic Roslyn/debugging parity; (b) **naked chat** without connection to the IDE session; (c) **unsecure** open port without pairing.

---

## 2. Decision (intention)

### 2.1. Three circuits (separation of responsibilities)

| Contour | Where | Destination |
|--------|-----|-----------|
| **Heavy (cockpit)** | CascadeIDE on workstation | editor, Roslyn, build, debugging, full HUD, MCP in progress |
| **Lightweight (remote operator surface)** | **PWA** on another device (phone, tablet, second PC) | session status, Intercom (limited), approve/reject, pause/stop, “agent is waiting” notifications |
| **Machine** | git, MCP remote, CI, Issues | closing a task without UI; agent and automation |

**Principle:** remote surface **does not** duplicate the cockpit. He **observes and decides**; execution of heavy tasks - on a station with IDE or through existing **MCP / `IdeCommands`** (with a policy on the gateway).

### 2.2. Remote operator surface (ROS) - scope v1

**Included (target phase 1-2):**

- Subscription to **snapshot** of session: workspace id, active Intercom topic/thread (briefly), “waiting for person” flag, last N agent messages, IDE health / failed step (without leaking the full workspace tree).
- Operator actions with **explicit confirmation** on the desktop in case of risk: response to chat (follow-up), approve/reject for “human only” zones ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md), pre-flight — [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) when it appears).
- **Push / pull notifications:** “agent is blocked on a question” (integration with an external channel - Telegram, email - **outside** the ADR core, via an adapter).
- **Pairing** of a remote client with an IDE instance (one-time code, TTL, recall).

**Not included in v1:**
- Full-fledged code editor in the browser.
- Randomly call all `IdeCommands` from the phone without allowlist.
- Public cloud relay without E2E / without being tied to a pair of devices.
- **Native** companion applications (iOS/Android/desktop) - postponed; if necessary later as a wrapper over the same API gateway.

### 2.2.1. Client: PWA (canon)

**Solution:** remote operator surface is implemented as a **Progressive Web App**, not as separate native applications and not as “just a tab without install”.

**Why PWA:**

| Criterion | PWA |
|----------|-----|
| One artifact | phone, tablet, second PC - one UI (HTML/CSS/TS), no store releases |
| Friction / worst way | “opened in browser → Add to Home Screen” without installing CIDE |
| Transport | the same **Operator Gateway** (SSE/WebSocket + REST); PWA - client only |
| Notifications (Phase 3) | **Web Push** + service worker with explicit consent; fallback - pull/SSE |
| Offline | service worker: UI shell cache + latest snapshot; **commands** when offline - queue or failure (see §7) |
| Security | console origin = origin gateway (loopback or VPN); **do not** mix with [0108](0108-web-ai-portal-host-object-tools-bridge.md) |

**Delivery:** static PWA is distributed **from the same host** as the Operator Gateway (for example `Features/OperatorGateway/www/` or embedded `wwwroot`), so as not to produce CORS and a second port unnecessarily. After pairing, the user saves the “Cascade Operator” shortcut.

**Minimum UX v1:** one “session” page (status + feed + response field); mobile-first layout; without complicated cockpit navigation.

### 2.3. Transport and border (Operator Gateway)

Between **CascadeIDE (host)** and **remote client** there is a logical **Operator Gateway** (process or built-in module):

1. **Outgoing Events** - subset of [0099](0099-ide-databus-typed-events-and-projections.md) / projections ([0045](0045-agent-chat-persistence-event-log-and-projections.md)): serialized DTO, no secrets and no full paths to `.env` / `ai-keys.toml`.
2. **Incoming commands** - narrow allowlist (`operator.reply`, `operator.approve`, `operator.reject`, `operator.pause_agent`, ...), mapping to existing IDE loops (Intercom, PFD confirmations), **not** direct arbitrary `ide_execute_command` from the Internet.
3. **Transport:** preferably **WebSocket or SSE + REST** on `localhost` with **TLS** and reverse proxy only when explicitly configured; default - **loopback** + pairing token.
4. **Recovery parity** - in spirit [0043](0043-mcp-transport-recovery-human-agent-parity.md): when the remote client breaks, it shows “session unavailable”, does not simulate success.

**Connection with [0108](0108-web-ai-portal-host-object-tools-bridge.md):** Web AI Portal - **foreign origin** in WebView2; ROS-PWA - **our own** origin gateway, separate allowlist. Do not mix portal and remote control.

### 2.4. Difference from multi-window [0017](0017-multi-window-workspace-and-agent-surfaces.md)

| | ADR 0017 | ADR 0117 |
|---|----------|----------|
| Devices | one PC, N monitors | N devices, 1 “heavy” station |
| UI | Avalonia `TopLevel` | **PWA** (canon) |
| Synchronization | shared process, shared `DataContext` | network/loopback gateway |
| Criterion | cockpit layout | surveillance + solutions outside the cockpit |

Both can work **simultaneously** (second monitor + phone as a remote control).

### 2.5. Friction criterion (negative scenario)

We consider ROS successful for scenario **S**:

1. The operator has moved away from the workstation; The remote control (or PWA) is open on the phone.
2. The agent in the IDE has requested clarification or confirmation.
3. The operator **sees** the request within a reasonable TTL and can **answer** or **reject** without installing full CIDE on the phone.
4. Agent **continues** on desktop; in the audit log there is a combination of `operator_device_id` + action.

If only “open laptop with IDE” is available for S, ROS **hasn’t** removed the friction for this class of tasks.

---

## 3. Phases (road map)

| Phase | Contents | Readiness criterion |
|------|-----------|---------------------|
| **0** | This ADR; list of DTOs and allowlists in a drawing | Borders agreed with 0017/0108/0031 |
| **1** | Read-only: gateway + **PWA** (manifest, service worker shell); snapshot + SSE | PWA on loopback: status and “waiting for operator” |
| **2** | Write + pairing; installable PWA on phone on LAN | Scenario S passes |
| **3** | Web Push / adapters; optional TLS + VPN to gateway | Operator outside LAN with secure channel |
**MVP-zero (outside CIDE code):** async via Issues / org Discussions / Telegram-relay MCP - **doesn't** replace ROS, acceptable as a temporary workaround.

---

## 4. Security and trust

- **Secrets** are not transferred to the remote client; `ai-keys.toml` and MCP tokens remain on the IDE station.
- **Pairing:** short-lived code; list of recalled devices; by default gateway listens to **127.0.0.1**.
- **Allowlist of commands** on gateway - separate from [0108](0108-web-ai-portal-host-object-tools-bridge.md) and from the full MCP.
- **Audit:** each remote action is logged with `principal=human`, `channel=remote_operator`, `device_id`.
- **Do not** expose the gateway to the Internet without an explicit org policy (VPN, mTLS).

---

## 5. Alternatives (rejected or postponed)

| Alternative | Why not the target path |
|--------------|-----------------------|
| **Full web IDE** | Roslyn/debugging parity; huge friction; duplicates cockpit |
| **RDP/VNC on desktop only** | Works, but not mobile-friendly; does not reduce friction scenario S |
| **Public MCP endpoint** without layers | Risk of workspace leakage; see backlog remote MCP in agent-notes |
| **Single chatbot without IDE connection** | There is no session truth; hallucination “I did” |
| **Native companion (v1)** | Two support circuits; PWA closes scenario S; native - if proven necessary |

---

## 6. Consequences

- A new **feature slice** will appear (for example `Features/OperatorGateway/`): host service, DTO, allowlist, **PWA statics** (`manifest.webmanifest`, service worker, UI).
- Requires **contract tests** snapshot for wire-DTO ([0008](0008-mcp-contracts-and-testable-infrastructure.md), [0052](0052-agent-contract-cli-and-snapshot-tests.md)).
- **Intercom / 0031 / 0042:** approve remote must converge with the same state machines as PFD/MFD on the desktop - not a second parallel “source of truth”.
- Operator documentation: separate page in handbook/kb-public **after** phase 1 (not in this ADR).

---

## 7. Open questions

1. **Built-in gateway** vs separate process `cascade-operator-gateway` (easier to update, harder to deploy)?
2. **One workspace - several remote clients** (family of devices) or one active remote control?
3. **Communication with ACP [0016](0016-agent-client-protocol-external-agent.md):** Does the remote surface control only the built-in IDE agent or also the external ACP?
4. **Offline:** command queue on the client vs hard failure?
5. Intersection with **[0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md)** (emergency / presence) - is it a general notification channel or a separate one?
6. **Web Push:** VAPID and delivery via gateway vs only external adapter (Telegram) in phase 2?

---

## 8. Implementation status

| Region | State |
|---------|-----------|
| ADR/borders | this document |
| Operator Gateway in code | no |
| PWA (manifest + SW + UI) | no |
| Pairing | no |
| DataBus/Intercom integration | no |

When the code appears, update §8 and the ADR status to **Accepted · Implemented** (by [status-lifecycle.md](status-lifecycle.md)).