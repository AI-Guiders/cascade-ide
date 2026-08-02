# Agent surface parity contract (v0)

- id: `agent-surface-parity-contract-v0`
- at_utc: 2026-08-01
- status: accepted direction (discuss → ship)
- products: `#CDP` · Glass WPF · (later) Qt/C++ Linux
- related: CIDE `ide_*` / `UiLayoutSnapshot` · ADR 0002 · 0012 · 0017 · MCP-PROTOCOL · glass-core-shared-carve (UiLayoutSnapshot was Defer)

## Axiom (two debts)

1. **CDP habitat** — agent's own IDE (buffer / seats / land / shell). Hard repayment of agent debt as *environment*.
2. **Shared surface channel** — CIDE-style co-presence in the human visual channel (layout / appearance / color under cursor / drive). Softer historically, still required.

Both kept. CDP does not replace (2).

## North star

- **Contract-first** toolkit-agnostic surface API (JSON + stable window roles).
- **WPF Glass** = first adapter (return full debt now).
- **Avalonia** = optional reference host; **not** a world to preserve. Do not pull Avalonia Visual into Core as SSOT.
- **Qt/C++ on Linux** = target adapter of the same contract.
- **CDP Meta** = agent wire (`go=` / soft organ), not Avalonia-only MCP ListTools.

## Full debt DoD (not sense-lite)

Agent from CDP can complete the same UI co-presence loop a human does on Glass.

| Class | Ops (CIDE names as intent) |
|-------|----------------------------|
| Sense | `layout` (all top-levels), `appearance`, `colors_under_cursor`, theme snapshot |
| Aim | `highlight` |
| Drive | `focus`, `click`, `set_text`, `send_keys`, `set_control_layout`, `set_panel_size` |
| Confirm | `request_confirmation`, preview windows — if in human UI path |

**Closed when:** full table works end-to-end on Glass via CDP, not when only colors exist.

Webcam / PrintWindow = escape sense only; **not** a substitute for this debt.

## Out of scope

- General OS HWND focus/move/resize/click for arbitrary apps.
- Keeping Avalonia `UiLayoutSnapshot` as the long-term SSOT (carve doc Defer stands; shape may be *referenced* for wire parity).
- Replacing CDP semantic habitat with UI-tree drive.

## Shape (v0 sketch)

Stable JSON (illustrative; refine in ship):

```json
{
  "schema": "agent_surface/v0",
  "windows": [
    {
      "role": "main|mfd_host|other",
      "title": "…",
      "is_active": true,
      "root": { "name": "…", "type": "…", "bounds": {"x":0,"y":0,"w":0,"h":0}, "visible": true, "content": null, "children": [] }
    }
  ]
}
```

Drive args: `name` (from layout) or under-cursor; same search order as CIDE (main then other top-levels).

Prefer plain JSON / latch-friendly payloads so Qt/C++ can speak the same dialect without .NET interfaces as the only ABI.

## Delivery order

1. Contract note (this) + CDP Meta stub (`cdp_glass` / `surface_desk` — name TBD).
2. WPF adapter: Sense → Aim → Drive → Confirm.
3. Dogfood: agent scenario on live Glass (layout → highlight → click/keys).
4. Qt adapter later — same JSON, no Avalonia port obligation.

## Antipatterns

- Sense-only ship claimed as "parity restored".
- Forking a second layout dialect for WPF vs Avalonia without a shared contract.
- Treating Glass projector peels (0-sync entities) as sufficient for surface debt.
- Blocking WPF on Avalonia migration into GlassCore.

## Decision

Accepted 2026-08-02 (operator): both debts; full surface debt; contract-first; WPF now; Qt north star; Avalonia not retained as world.
