<!-- English translation of adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md. Canonical Russian: ../../adr/0072-chat-topic-cards-intent-melody-keyboard-contract.md -->

# ADR 0072: Chat topic cards, drill-in/back and intent-based Melody/Chords for topic navigation

**Status:** Accepted · Implemented  
**Date:** 2026-04-19  
## Related ADRs

| ADR | Role |
|-----|------|
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | refinement packages, `ThreadNode`, scope review |
| [0057](0057-chat-surface-pipeline-adoption.md) | chat surface → Skia pipeline |
| [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) | Melody, CascadeChord, parity with palette - **to be specified in chat-domain**, see below |
| [0013](0013-command-surface-and-discoverability.md) | Command surface and discoverability (palette, minimal toolbar) |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | Layers of team identifiers, hotkeys and UI (without one “all-in-one” table for now) |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | multi-window, focus |
| [0070](0070-command-palette-direct-overlay-surface.md) | Command Palette as direct overlay surface routed to active TopLevel |
| [0044](0044-avalonia-host-skia-agent-chat-surface.md) | Separation of roles - Avalonia as a host (“fuselage”), custom rendering for agent chat (Skia as a hypothesis) |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Stable MCP contracts and testable infrastructure |
| [0096](0096-intercom-topic-card-summary-and-product-spine.md) | **summary on card**, product line spine - product semantics on top of overview/detail; CIDE as an example; [§4](0096-intercom-topic-card-summary-and-product-spine.md#adr0096-p4) |
| [0119](0119-chat-slash-commands-intercom-surface.md) | Slash commands in `ChatInput` - same `command_id` as Melody/Chords/palette |

### Outside ADR

| Document | Role |
|----------|------|
| [intent-melody-language-v1.md](../../intent-melody-language-v1.md) | IML v1: `c:` grammar and motivation |
**Relation to [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md):** This ADR **does not replace** the generic keyboard-first model (palette, `CascadeChord`, Command Melody `c:`). It **normatively clarifies** how the same principles **apply to navigating chat topics** (topic-level intents). See [§ “Relation to ADR 0060”](#adr0072-relation-0060).

---
## Context

[0031](0031-agent-chat-clarification-batches-and-threading.md) already introduces **threads as persistent lines of work** (`ThreadNode` vs `MessageNode`), refinement batches and a **"sweep overview"** vector of sessions instead of one endless thread. [0057](0057-chat-surface-pipeline-adoption.md) transfers the chat to the common pipeline **Intent → Declutter → Layout → Render** and allocates domain nodes (`ThreadNode`, `MessageNode`, ...) as first-class at the Intent stage.

However, this is **not enough for a product UX contract**: branching data can exist in the model, and the **default screen** is still perceived as **one linear feed**. Not recorded:

- explicit **screen model** “topic overview → topic entry → back”;
- **adaptive default** (one theme vs several);
- **mandatory** for v1 layer **keyboard navigation by topic** through **the same** `command_id` as the palette and Melody/Chords ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)), without reference to control coordinates.

---

## Problem

1. **Data and navigation gap:** `ThreadNode` and snapshot layout ([0057](0057-chat-surface-pipeline-adoption.md)) describe the graph; without an explicit **view mode** (overview vs detail), the user remains in the “one feed” mental model, even when there are several topics.
2. **Lack of a drill-in/back canon:** exiting a topic should not return to the abstract “general feed” if the product model is **topic cards** as a primary review.
3. **Keyboard-first without an intent layer:** if Melody/Chords **directly** pull the focus of Skia elements or hitboxes, cross-platform and parity with MCP are lost ([0008](0008-mcp-contracts-and-testable-infrastructure.md)); The logic with pointer is duplicated.
4. **Mixing with dangerous confirmations:** topic navigation should not compete in UX with **PFD confirmations** and clarification packages as “yes/no” - the boundaries remain at [0017](0017-multi-window-workspace-and-agent-surfaces.md) and [0031](0031-agent-chat-clarification-batches-and-threading.md).

## Summary
- **Topic cards** in chat: drill-in/back, adaptive default; intent-based Melody/Chords v1.
- Clarifies [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) chords in chat-domain.
- Complements Intercom/spine ([0096](0096-intercom-topic-card-summary-and-product-spine.md)).

---

## Solution

<a id="adr0072-p1"></a>

### 1. Topic cards as the primary form of multi-topic chat overview

- **The thread card** represents a **threadnode** rather than a separate message.
- With **one** topic in a session, it is permissible to open the chat immediately in **detail** (timeline of this topic).
- With **several** topics **default** — **overview** from topic cards (see [§3](#adr0072-p3)).
- **Main line** (main thread) remains **one of the cards**, and not a hidden “mode without cards”.

<a id="adr0072-p2"></a>

### 2. Drill-in / Back as a canonical navigation model

- **Drill-in:** selecting a topic reveals it as a **detail timeline** (messages and related content within the topic).
- **Back:** returning from detail leads to **topic overview**, and not to a separate “global feed” without cards.
- The story within the topic remains **chronological**; branches are reflected by model data ([0031](0031-agent-chat-clarification-batches-and-threading.md)), and not by arbitrary “jumping” along the UI tree.

<a id="adr0072-p3"></a>

### 3. Adaptive default view

| Condition | Default view |
|--------|----------------|
| One thread (or the equivalent "focus on main thread only") | Detail timeline |
| Multiple topics | Overview (cards) |

The exact rules “when to consider a topic as one” are the subject of implementation in the VM; ADR captures the **product principle**, not the detection algorithm.

<a id="adr0072-p4"></a>

### 4. Intent-based Melody and Chords as a mandatory v1 keyboard-first layer for chat topics

- Topic navigation **must** have inputs: **direct commands** (palette/toolbar), **CascadeChord** ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)) and **Command Melody `c:`** ([0060 §11](0060-keyboard-chord-stack-fms-tactical-strategic.md#adr0060-p11)) over **one** set of `command_id` ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)).
- **Melody** and **Chords** for chat-topic navigation **do not address** specific controls, coordinates or internal `HitTarget`; they call **chat navigation intents** (see [§5](#adr0072-p5)).
- **Minimum set v1** (identifiers for inclusion in the `IdeCommands` / registry; here - logical names):

| Intent | Destination |
|--------|-----------|
| `focus_chat_surface` | Focus on the chat surface (MFD/host), without changing the topic |
| `focus_next_topic` | Focus on the next topic in an overview or logical “next thread” within the focus contract |
| `focus_previous_topic` | Likewise back |
| `enter_focused_topic` | Transition from overview to detail of the selected topic |
| `return_to_topic_overview` | Exit from detail to overview |

Expanding a set (for example, “collapse branch”, “rename topic”) - with separate commands and ADR if necessary.

Parity with [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md): any command accessible only from a chord or only from `c:` must be discoverable via the palette and MCP with the same `command_id` ([0060 §9 discoverability](0060-keyboard-chord-stack-fms-tactical-strategic.md#adr0060-p9)).

<a id="adr0072-p5"></a>

### 5. Intent-first interaction contract

- **Stream:** `Melody / Chords / palette / MCP` → **intent** (`command_id`) → **VM state** (including view mode, focused thread) → **layout** ([0057](0057-chat-surface-pipeline-adoption.md)) → **render** (`SkiaChatSurfaceControl`).
- The same intent **should** have a predictable effect in **overview** and **detail** (for example, “next topic” in detail can be interpreted as “next among the visible topics of the session” - the specific semantics are set by the VM, but **not** duplicated by a separate shortcut layer for each mode for no reason).
- **Pointer/tap** to a card or back button **reduces to the same intent commands** as the keyboard, and not to parallel navigation logic.

<a id="adr0072-p6"></a>

### 6. Separation of concerns
- **Intent / pipeline** ([0057](0057-chat-surface-pipeline-adoption.md)): knows topics, messages, confirmations and connections; builds `ChatSurfaceState`.
- **Layout:** decides which regions are overview vs detail and which are lanes/entries ([`ChatSurfaceLayout`](../../Features/Chat/ChatSurfaceSnapshot.cs), [`ChatThreadOverviewItem`](../../Features/Chat/ChatSurfaceSnapshot.cs)).
- **Render** ([`SkiaChatSurfaceControl`](../../Views/SkiaChatSurfaceControl.cs)): rendering and hit-testing **without** calculating domain branching from geometry bypassing snapshot.

---

<a id="adr0072-relation-0060"></a>

## Communication with ADR 0060

- [0060](0060-keyboard-chord-stack-fms-tactical-strategic.md) remains **canon** for: **three command surfaces** (palette, `CascadeChord`, slash in Intercom — [0119](0119-chat-slash-commands-intercom-surface.md) §1a), S/T/M/E axes where applicable, overlay, Command Melody `c:`, registry parity.
- **0072** introduces **additional product contract** for **chat-topic navigation** only and **does not** override the general chord/melody model.
- Formulation of the **“amended in part”** level: for the **chat domain** overview/detail, topic cards and a **minimal** set of intent commands with mandatory Melody/Chords/palette parity are specified; the rest of **0060** is unchanged.

---

## Implementation anchors (code)

| Component | Role |
|-----------|------|
| [`ChatPanelViewModel`](../../Features/Chat/ChatPanelViewModel.cs) | View mode (overview/detail), selected/focus thread, intent command execution, snapshot update |
| [`ChatSurfaceSnapshot`](../../Features/Chat/ChatSurfaceSnapshot.cs) / [`ChatThreadOverviewItem`](../../Features/Chat/ChatSurfaceSnapshot.cs) | Layout level entities for overview cards and tracks |
| [`ChatSurfaceCompositor`](../../Features/Chat/ChatSurfaceCompositor.cs) | Splitting pipeline: overview vs detail layout on top of `ChatSurfaceState` |
| [`SkiaChatSurfaceControl`](../../Views/SkiaChatSurfaceControl.cs) | Interactive topic cards, hit targets, affordance “back” - in terms of snapshot, not domain logic |
| [`MainWindowHotkeyService`](../../Services/MainWindowHotkeyService.cs), [`MainWindowViewModel.CascadeChord`](../../ViewModels/MainWindowViewModel.CascadeChord.cs) | Binding gestures to `command_id` ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md)) |
| `IdeCommands` /registry | Canonical `command_id` strings for intent from [§4](#adr0072-p4) (e.g. [`IdeCommands.PowerDocuments`](../../Services/IdeCommands.PowerDocuments.cs) and adjacent partial classes as commands are added) |

---

## Flow diagram (intent → state → layout)```mermaid
flowchart LR
    keyboardInput[KeyboardInput] --> melodyChordParser[MelodyChordParser]
    melodyChordParser --> chatNavIntent[ChatNavigationIntent]
    pointerInput[PointerInput] --> chatNavIntent
    chatNavIntent --> chatViewState[ChatViewState]
    chatIntentPipeline[ChatIntentPipeline] --> chatViewState
    chatViewState --> overviewCards[OverviewCards]
    chatViewState --> topicDetail[TopicDetail]
```
---

## Non-targets (v1)

- Free **mind-map** layout of themes without separate ADR and focus assessment.
- **NLP-based** auto-detection of topics as a mandatory UX base.
- Replacement of the [0057](0057-chat-surface-pipeline-adoption.md) pipeline or abandonment of the Skia product path.

---

## Risks

- Reload v1 with **full graph layout** instead of cards and linear detail.
- Unclear **focus** between `TextBox` input, chat surface and palette ([0013](0013-command-surface-and-discoverability.md), [0017](0017-multi-window-workspace-and-agent-surfaces.md)).
- **Hidden** keyboard UX, if commands are not visible in the palette and in help ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)).
- Leaking **UI-bound shortcuts** bypassing `command_id`.
- Mixing **dangerous confirmations** with topic navigation - continue to separate [0017](0017-multi-window-workspace-and-agent-surfaces.md) and [0031](0031-agent-chat-clarification-batches-and-threading.md).

---

## Consequences

- Addition of **explicit navigation state** in chat VM and overview/detail tests.
- Updated **command registry**, `hotkeys.toml` (spike and custom merge) and **melody alias** for topic intents.
- Better **session orientation** at the cost of a more rigorous focus model and user documentation.

---

## Provenance and formalization

Product ideas **topic cards**, **drill-in/back**, **intent-first Melody** for chat were developed in **live dialogue with the user** and here **formalized as the architectural canon** of the repository. External products (including research lines like Comet/Perplexity) may be referred to in other documents as **context of wording**, but the **normative source** for CascadeIDE remains **this ADR** and associated ADRs in `docs/adr/`, not exporting external chats.

---

## Implementation plan (after ADR adoption)

1. Add navigation state to `ChatPanelViewModel`.
2. Divide the layout in `ChatSurfaceCompositor` into overview and detail.
3. Rebuild `SkiaChatSurfaceControl` for topic cards + drill-in/back.
4. Enter intent-level chat navigation commands and register in the palette.
5. Add Melody/Chord bindings to the same `command_id`.
6. Tests: overview/detail, keyboard contract, palette/MCP parity.