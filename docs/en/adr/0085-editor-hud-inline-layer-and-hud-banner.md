<!-- English translation of adr/0085-editor-hud-inline-layer-and-hud-banner.md. Canonical Russian: ../../adr/0085-editor-hud-inline-layer-and-hud-banner.md -->

# ADR 0085: Editor HUD - inline layer in the editor and difference from the HUD banner

**Status:** Proposed  
**Date:** 2026-04-20

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | PFD/MFD - Cascade IDE Cockpit Attention Model |
| [0032](0032-hud-banner-configuration-and-grammar.md) | config and grammar **bars** above the editor |
| [0066](0066-cockpit-ui-vs-ide-presentation-layer.md) | Cockpit UI vs presentation IDE |
| [0079](0079-ide-display-system-ids-overlay-pipeline.md) | IDS - application overlays, not to be confused with the HUD editor |
| [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md) | agent presence, cursor, single buffer |

---
## Context

In [0021](0021-pfd-mfd-cockpit-attention-model.md) **HUD** is described as a layer **inside the windshield** (editor): inline diagnostics, ghost text, gutter, if necessary - **file-level banner** above the text. The same thesis (“not the fourth anchor”, layer inside the editor) was already in the context of [0032](0032-hud-banner-configuration-and-grammar.md); this ADR does not rewrite [0021](0021-pfd-mfd-cockpit-attention-model.md), but only splits the dictionary. In conversation and in code, three different concepts are mixed:

1. **Editor HUD** (product term for this ADR) - everything that is perceived **in the editor plane** next to the code and the caret: underlines, inlay hints, Quick Info at the pointer, ghost text, indicators in gutter, later - overlays tied to **document coordinates**.
2. **HUD banner** - a narrow **bar above the text** (under the document tab), file-level summary (diagnostics, “the agent is editing the file,” etc. according to [0021 §9 table](0021-pfd-mfd-cockpit-attention-model.md)); in the code - `EditorHudBannerText` / banner visibility in the doc. This is a **private view** of chrome around the editor, not a replacement for the inline layer.
3. **Global IDE overlays** (palette, toast, modals) - outline [0079](0079-ide-display-system-ids-overlay-pipeline.md); they are **not** included in the Editor HUD, even if visually "on top of the editor".

Without explicit names, the team and users say “HUD” and mean different things: some say a stripe above the editor, others mean an experience like VS (carriage types, inlays). This ADR fixes the **canon of terms** and **limits of responsibility**, without promising a full set of implementations.

## Solution

<a id="adr0085-p1"></a>

1. **Canonical name “Editor HUD”** denotes **inline layer and document-linked overlays** inside the frontal editor: minimum - on-site diagnostics, ghost text, gutter; target horizon - types/Quick Info at the carriage, inlay hints, method parameters - in the spirit of “don’t look away at the sidebar” ([0021 §9 concept](0021-pfd-mfd-cockpit-attention-model.md)).

<a id="adr0085-p2"></a>

2. **HUD banner** - a separate name for the **file-level strip** above the text. It is **optional** relative to the Editor HUD: it can duplicate a short signal (for example, a diagnostic summary) while the inline does not yet cover the scenario, or remain for **file level events** (file agent activity), without crowding out the development of the inline layer.

<a id="adr0085-p3"></a>

3. **The principle from [0021 §9](0021-pfd-mfd-cockpit-attention-model.md)** remains: Editor HUD elements do not **block** typing, are visually **easier** than the main code, **can be disabled** without losing a critical signal in the PFD/EICAS, if it is duplicated there.

<a id="adr0085-p4"></a>

4. **Configuration:** content and template of **banner** - by intent [0032](0032-hud-banner-configuration-and-grammar.md) (`settings.toml`, grammar). Expandable **visibility policies** Editor HUD (what to show inline, density, “only with delay”) - separate keys/sections as implemented; do not mix with the **presentation** windows config ([0017](0017-multi-window-workspace-and-agent-surfaces.md)).

<a id="adr0085-p5"></a>

5. **Communication with the agent:** presence (cursor, “writes”), single buffer - [0084](0084-agent-edits-editor-source-of-truth-presence-channel.md). Visually, ghost text and gutter indicators belong to the **Editor HUD**; chat status - no.

## Pivot table (terms)

| Term | Where does he live | Examples |
|--------|----------|---------|
| **Editor HUD** | Editor/Document Plane | Squiggles, ghost text, gutter icons, inlays, Quick Info at the carriage |
| **HUD banner** | Bar above text in dock | Diagnostic summary, file-level agent message |
| **IDS overlay** | Application host, not document model | Command Palette, modals, global toast ([0079](0079-ide-display-system-ids-overlay-pipeline.md)) |

## Consequences
- Documentation, ADR and discussions use **Editor HUD** vs **HUD banner** predictably; references to “HUD” without specification in the regulatory text should be avoided or disclosed according to the table above.
- The product roadmap can **prioritize the Editor HUD** (inline) regardless of the fate of the banner; the banner is not advertised as the only "HUD" implementation.
- New overlays solve: **binding to document/carriage** → Editor HUD layer and editor engine; **global UI** → IDS / individual contours by [0079](0079-ide-display-system-ids-overlay-pipeline.md).

## Rejected / deferred alternatives

- **One word HUD for everything** - left as colloquial; in ADR and code, the clarifications **Editor HUD** / **HUD banner** are preferred.
- **Consider the banner the only HUD** - rejected: contradicts [0021 §9](0021-pfd-mfd-cockpit-attention-model.md), where inline is listed first in the sense of the “main” HUD.

## Open questions

- Single **data pipeline** for Quick Info / inlays (Roslyn, LSP) vs disparate services - separate ADRs as they are connected.
- Does the user need the setting **"banner off, inline on"** as a default preset after inline maturity - a product solution.