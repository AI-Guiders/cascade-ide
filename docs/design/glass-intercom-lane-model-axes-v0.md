# Glass Intercom · lane × model axes (v0)

**Status:** accepted 2026-08-05 (operator + agent).
**Scope:** Glass Intercom chrome — not ChatPanel Avalonia settings.
**See also:** [glass-intercom-northstar-messenger-v0](glass-intercom-northstar-messenger-v0.md) — Intercom → team messenger/channels; lane strip is near-term, may become channel rail.

## Decision

Two axes stay **separate controls** (do not overload one ComboBox):

| Axis | Meaning | Where | Control |
|------|---------|-------|---------|
| **Lane** | Habitat seat: who receives / which pipe | Composer strip (у Send) | **XOR Korry strip** — not ComboBox |
| **Model** | FM / MAF model id for Citizen path | Intercom HUD (сверху, у HDG/CRS) | ComboBox (live catalog) |
| **Provider secrets** | key, baseUrl, default model | CFG / `AiChatSettings` | settings host |

## Lane UI (organic)

Three fixed seats → radio/Korry, not dropdown:

- Buttons: `CIT` · `HOST` · `PF` (tooltips: Citizen · Composer · PF · habitat).
- Style: same flat `KorryBtn` language as AUTOI/HILD/VAD (lit = selected).
- Mutual exclusive; sticky latch (reuse current ModelPicker persistence path, rename latch to lane).
- Replace current `ModelPicker` ComboBox at Send.

Why not ComboBox for lane: three known seats, high frequency, cockpit metaphor already on HUD. ComboBox reads as "pick from catalog".

## Model UI

- HUD right of HDG/CRS: ComboBox with short model id (+ quiet provider chip if needed).
- Source: OpenAI-compatible `GET /v1/models` via existing `FmModelCatalog`; other providers = thin adapters later.
- **Lit** when lane = `CIT` (MAF/Citizen path). On `HOST` / `PF` → dim / `—` (no fake list).
- MAF/`IChatClient` = chat abstraction only; model list is not MAF — catalog stays provider API.

## CFG

Sticky default provider + key + default model. Intercom HUD model = session override. Do not thrash secrets in Intercom chrome.

## Antipatterns

- One ComboBox labeled "Model" that mixes lane + FM id.
- Two ComboBoxes side-by-side at Send.
- Showing live FM catalog when lane ≠ Citizen.
- Inventing unified `ListModels` on `IChatClient`.

## Ship notes (later leaf)

1. Rename latch/API: `model_choice` → `intercom_lane` (or keep file, change semantics).
2. Lane Korry strip + remove lane ComboBox.
3. HUD model ComboBox + wire `FmModelCatalog` when lane=CIT.
4. PNG + `cdp_see` dogfood before Done.
