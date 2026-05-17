<!-- English translation of adr/0108-web-ai-portal-host-object-tools-bridge.md. Canonical Russian: ../../adr/0108-web-ai-portal-host-object-tools-bridge.md -->

#ADR 0108: Built-in web portal for external web AI and tool bridge via Host Object (WebView → IDE)

**Status:** Accepted · Implemented  
**Date:** 2026-05-10  
**Fixed parameters (2026-05-10):** format **`executeIdeCommand(string json)`**, PoC-allowlist for reading, Read / Write-confirm modes, WebView2 model below the page (see §2) - agreed for the first integration into **CascadeIDE**. *Sidenote:* the **Atlas** person sometimes gives “sounding” working names in the discussion - this is rhetoric, not individual entities in the repository archive, unless they are clearly listed as a product.

## Related ADRs

| ADR | Role |
|-----|------|
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | basic boundary "web ≠ automatic MCP"; **this ADR** specifies an *explicit, consistent* channel on top of it |
| [0093](0093-mfd-embedded-browser-for-launch-url.md) | MFD/browser/launch URL |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | `IdeCommands`, MCP, IDE Executable Circuit |
| [0038](0038-agent-facade-ai-provider-and-tool-orchestration.md) | provider facade and tool orchestration |
| [0016](0016-agent-client-protocol-external-agent.md) | external agents; orthogonal |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | body surface / parity |
## Summary

- **Web AI portal** on MFD: WebView2, Host Object → `IdeCommands`/MCP.
- Allowlist, user consent; PoC (Atlas/Search AI).
- Trust boundary - [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md).


---

## 1. Context

**Powers:**

- The host editor (for example Cursor) periodically goes into **Reconnect** or crashes - long sessions and stable access to tools suffer.
- **Local** LLMs on a typical workstation give a **long track** of response; Not everyone has the option of purchasing a dedicated GPU in the near future.
- At the same time, **cloud web interfaces** from vendors (including “AI in search” modes) can provide acceptable speed and quality of reasoning **without** being tied to the release cycle of the local stack.

**Limitation:** Web AI lives in the **origin sandbox** of someone else's domain; The page **doesn't** have the same API as the native agent with MCP in the IDE process. In [0035 §2–3](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md#adr0035-p2) it is fixed: arbitrary HTTPS-origin **does not** automatically gain access to MCP, workspace files and secrets. In [0035 §5](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md#adr0035-p5) it is noted that **connecting the web with local tools** is a separate line with a consent model and threat analysis.

**Observation:** for the scenario “the web model reasons, and the edits/assembly/navigation is done in the IDE,” a **narrow, auditable bridge** is sufficient: the page does not call arbitrary host code, but a **stable contract** (`execute_ide_command` / `command_id` + JSON args), which converges with an existing one inside the IDE **`IdeMcpCommandExecutor`** and canon [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md).

**Empirics:** carried out **PoC** together with **Atlas** on the web AI side (including targeting **Google Search AI** / web shell without a full-fledged local tool runtime on the page). The **transport** link has been proven: the signal from the web reaches the **native bridge** (Host Object), then in the PoC the output goes **echo to a separate console application listening to port `:8080`** - **without** calling real **IDE tools** and **without** `IdeMcpCommandExecutor`. That is, the **idea of ​​bridge and injection** was tested, and not the end of the “command → workspace” chain. Connecting to the canon [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) and whitelist §2.2 is the **next** step (M1).

---

## 2. Decision (intention)

### 2.1. Single call with JS: `executeIdeCommand(string json)`

<a id="adr0108-p1"></a>

1. **One method on the Host Object boundary** - without a mirror “one method for each `ide_*` tool.” Recommended signature for **AddHostObjectToScript**: **`executeIdeCommand(string json)`** (or equivalent with the same request body).

2. **JSON body** - the same contract as the MCP manager: an object with **`command_id`** and **`args`** (as in [MCP-PROTOCOL.md](../../MCP-PROTOCOL.md) / parsing `IdeMcpServer`: top-level merge with nested `args` is possible). Parsing, validation and routing remain **C# only**; on the web page side there is one simple interface.

3. **JSON transport** is required: field expansion without rebuilding the bridge; new commands are added through **`IdeCommands`** and allowlist, rather than through new COM methods.
#### Recommended model output: fenced `json-cascade`

Ready text of instructions for attachment to the operator’s external web chat: **`AiPrompts/web-ai-portal-bridge-adr0108.prompts.md`** (sections `## web_portal_bridge_*`). If the web layer spoils Markdown when copied from the chat history - a flat version without markup and with **one-line** JSON like the bridge buffer: **`AiPrompts/web-ai-portal-bridge-adr0108.chat-paste.txt`**.

To separate the command to the IDE from the arbitrary fenced `json` in the same answer, the web layer negotiates with the model: **one object** `{ "command_id", "args" }` (same contract as `ide_execute_command`) is placed in a fenced block with **info string** **`json-cascade`**:````markdown
```json-cascade
{ "command_id": "codebase_index_search", "args": { "query": "Foo", "top_n": 10 } }
```
````

IDE parser: `WebAiPortalJsonCascadeFence.TryExtractFirst` → JSON string → `invokeCSharpAction` / bridge. JSON must not contain the fence closing sequence (usually not an issue).

### 2.2. PoC allowlist (`command_id` whitelist)

<a id="adr0108-p2"></a>

4. **Primary safety guard** — a stable **whitelist** of permitted `command_id` values. Until the loop is debugged, commands **outside the list** are rejected in the IDE before the executor runs.

5. **Initial PoC set (eyes only, no destructive actions):** three semantics; CascadeIDE code uses **canonical identifiers** from `IdeCommands`:

   | Semantics (discussion shorthand) | Canonical `command_id` in CascadeIDE |
   |--------------------------------|----------------------------------------|
   | read / view editor context (read analog) | `get_editor_content_range` (with `get_editor_state` when active-file metadata is needed) |
   | search local code index | `codebase_index_search` · for stable web-chat flow, read-only **`codebase_index_status`**, **`codebase_index_explain`** are on the bridge whitelist |
   | diagnostics for current file | `get_current_file_diagnostics` |

   Aliases such as `read_file` / `search_index` / `get_diagnostics` are **not** a second source of truth in the product: if needed, only a thin **alias** in the JS layer mapping to the table above.

### 2.3. Modes: Read-only and Write-confirm

<a id="adr0108-p3"></a>

6. **Read-only:** commands classified as read/observe (in PoC — entire whitelist §2.2) run **without extra dialog** (after initial bridge enable and allowlist consent).

7. **Write-confirm (default):** any command classified as **mutation** (file write, git with side effects, build, delete, etc.) **does not** run silently: IDE shows **explicit confirmation** (modal or equivalent toast with Allow / Deny) with clear intent description (“agent requests X”). This fixes **proposed → human confirmed** model.

8. **Write without per-step confirmation (must-have for flow):** confirming **every** write separately is unacceptable for long sessions. There must be a separate, **explicit** option **“allow all writes for this bridge session”** / **“trust writes from this bridge (until revoked)”** — like *Run everything* / Cursor analog: one conscious opt-in, then allowlisted write commands run **without modal per call** until user **disables mode**, **revokes bridge**, or **closes session** (UX details — product; time/scope limits — settings). Default for newcomers remains **confirm each write** or narrow mode.

9. Read vs write classification — in bridge configuration (table or attribute on `command_id`); whitelist extension must include **classification**, else default — **write-confirm** (safe fallback).

### 2.4. Technical assumptions: WebView2 and “ordinary web” limits

<a id="adr0108-p4"></a>

10. **Embedding and placement:** built-in portal = controlled **WebView2** (or equivalent) inside IDE. **Canonical slot** for long-lived “web chat + bridge” — **MFD** (secondary attention contour per [0021](0021-pfd-mfd-cockpit-attention-model.md); aligns with [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) — embedded browser as deliberate zone, not fourth “forward” semantic anchor). **PFD** not reserved for permanent web chat. Alternative — separate window / secondary surface per [0017](0017-multi-window-workspace-and-agent-surfaces.md) and UI presets ([0010](0010-ui-modes-toml-configuration.md)).

11. **Bridge injection:** IDE tool interaction via **native object published to JavaScript** (e.g. **`AddHostObjectToScript`**) — call goes to **host-added object**, not `postMessage` between windows or page network stack. From page view this is **direct** JS → C# in IDE process.

12. **Same-Origin Policy and third-party page CSP** do not define **host object** access: origin policy still limits page network, but **does not remove** IDE responsibility for *what* is exported on `window` and *which* whitelist applies on C# side. “SOP bypass” for **network** does not justify weakening allowlist.

### 2.5. Deferred: operator presence (`IsHumanPresent` and similar)

<a id="adr0108-p5"></a>

13. **Out of scope for implementation in this ADR.** Separate product/engineering line: “human at machine” detection, simplicity, peripherals, link to [0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) — large effort; **not** in M1–M2 roadmap §7. Revisit after stable bridge (whitelist + Write-confirm). Presence heuristics **do not replace** explicit confirmation for write commands.

### 2.6. Consent, audit, executor

<a id="adr0108-p6"></a>

14. Before first bridge use — **explicit user consent** on allowlist and modes; **revoke** and **pause** bridge are mandatory.

15. **Where consent is shown (and chat):** main scenario — **dialog with web AI runs in embedded WebView2**, so bridge onboarding, mode banner, and buttons like “allow writes for session” **belong in the same attention zone**. Allowed implementations: **injection** into document (script / overlay / startup **wrapper URL** under IDE control), or **native** strip or modal **around** `WebView` in same slot (typically **MFD**). In all cases **fact** “bridge active / write-all for session” is stored and checked by **IDE process**; WV2 rendering is UX, not source of authority.

16. **Execution:** each call goes through same contour as MCP: `command_id` validation, whitelist, read/write class, UI marshaling ([0004](0004-ui-thread-marshaling.md)), quotas/timeouts, logging without leaking secrets to page console.

17. Source of truth for arguments — **`IdeCommands` + ProtocolDocGen**; no need to duplicate semantics in JS.

---

## 3. Relation to ADR 0035

- [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) remains: **arbitrary** page without bridge is **still not** an MCP client.
- This ADR describes **designed exception**: bridge enabled **deliberately**, with **limited** surface and **same** command executor as IDE agent — without mixing web provider origin with full process privileges.

---

## 4. Threats and mitigation (minimum)

| Threat | Mitigation |
|--------|------------|
| XSS on web provider side runs bridge calls as user | Narrow allowlist; optional confirmation on sensitive `command_id`; bridge revoke |
| API spoofing or extension via third-party script load | Bridge only on **designated** pages/presets; do not export arbitrary COM objects |
| Path/content leak to vendor cloud | Aligned with [0035 §3](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md#adr0035-p3): context to web — only explicit user actions + product policy |
| “All writes without prompt” mode (§2.3 item 8) increases **blast radius** on XSS/page session hijack | Only after **explicit** opt-in; visible UI indicator; **revoke** in one action; allowlist still limits command set |

Full threat model and pentest plan — outside this document; with M1+ rollout allow appendix or separate security ADR.

---

## 5. Rejected / deferred options

- **Rely only on copy-paste between web tab and IDE** — enough for “second opinion”, **not enough** for “direct tool control” without manual relay each step.
- **Give page direct socket to local MCP server** — mixes browser and MCP trust models; rejected in favor of **single executor inside IDE process**.
- **Unify web AI and native agent into one indistinguishable UX entity** — rejected; user must know **who** executes command (bridge under IDE control vs external host).
- **`IsHumanPresent` / auto read-only when operator absent** — **deferred** (separate line, see §2.5); does not block bridge from this ADR.

---

## 6. Consequences

- **Product** feature: “web persona + IDE tools” without mandatory host-editor dependency or local GPU.
- Implementation needs **WebView**, **bridge**, **consent UI**, **contract tests** bridge ↔ `IdeCommands` (snapshots or table tests).
- User docs must **clearly** distinguish: *native chat / MCP in IDE* vs *web portal with bridge* vs *ordinary external browser*.

---

## 7. Roadmap (normative minimum)

1. **M0 — transport PoC:** done (web → bridge → echo to console listener **:8080**; see §1); optionally record artifacts outside ADR.
2. **M1 — product integration:** `executeIdeCommand(string json)` → **`IdeMcpCommandExecutor`** + whitelist §2.2 + Read / Write-confirm + **“all writes for session”** mode (§2.3 item 8) + consent + logging (first **real** IDE tool call from bridge).
3. **M2 — hardening:** abuse metrics, whitelist review on `IdeCommands` updates. Presence policy (§2.5) — **not** in M2.

---

## 8. Implementation slice (M1)

- **WebView:** package `Avalonia.Controls.WebView`, control `NativeWebView` (Win — WebView2 underneath), no WinForms.
- **MFD:** `MfdShellPage.WebAiPortal`, view `Views/WebAiPortalMfdPageView.axaml`.
- **Bridge:** `Features/WebAiPortal/Application/WebAiPortalCommandBridge.cs` → `IIdeMcpActions.ExecuteCommandAsync` with whitelist §2.2; transport from page — Avalonia `WebMessageReceived` / `invokeCSharpAction` (body — same JSON with `command_id` and `args`). **AddHostObjectToScript** for sync JS↔C# on Windows if needed — via `TryGetPlatformHandle` / `ICoreWebView2` (Avalonia “Embedding web content” docs), does not block M1.
- **Navigation:** IDE command `show_web_ai_portal_page` → MFD region + `WebAiPortal` page.
- **Manual execution (buffer/button):** button “Execute command: buffer → last on page” (`WebAiPortalMfdPageView`) — (1) clipboard: fenced `json-cascade` **or** bare JSON `{ "command_id", … }` (common “copy” in chat UI without backticks); (2) else `NativeWebView.InvokeScript`: last matching block in `pre` / bare `code` in DOM (`WebAiPortalLastCommandDomProbe`). Then same path as `invokeCSharpAction`; result in `WebAiPortalLastBridgeResult`.
- **Hands-free without copy-paste and without that button:** checkbox “Auto: last json-cascade on page → bridge (poll…)” when **consent** and **bridge enabled** (`WebAiPortalMfdPageView`): `DispatcherTimer` ~1.1s runs same DOM probe (`WebAiPortalLastCommandDomProbe`); successful execution **deduped** by canonical JSON (`WebAiPortalBridgePayloadDedup`); on navigation dedup resets — same command can run again on new page. Probe handles fenced ```json-cascade and **bare** marker `json-cascade` + newline + JSON (typical Gemini / Google AI search without triple backticks); search uses `document.body.innerText`, not only `pre`/`code`.
- **Vendor “Send” click** is **not** automated: “Inject into composer” or manual send in site chat field remains (DOM/policy limit, not bridge blocker).
- **Inject IDE response into web chat (after successful bridge):** flags — (1) text to system clipboard; (2) optional `InvokeScript` / `WebAiPortalComposerInjectScript` into page focus. If response **very long** (typical `get_editor_state` with large preview) and **“under chat limit (~1200)”** enabled (default): buffer/composer gets **compact** with ready `json-cascade` for **HCI** (`codebase_index_search`, `codebase_index_status`, `codebase_index_explain`) and narrow read (`get_editor_state` with `max_preview_chars: 0`, `get_editor_content_range` near caret). Bridge whitelist includes **`codebase_index_status`** and **`codebase_index_explain`** (read-only).
