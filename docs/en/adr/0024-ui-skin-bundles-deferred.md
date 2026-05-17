<!-- English translation of adr/0024-ui-skin-bundles-deferred.md. Canonical Russian: ../../adr/0024-ui-skin-bundles-deferred.md -->

# ADR 0024: Skin bundles - postponed

**Status:** Deferred  
**Date:** 2026-04-11

## Related ADRs

| ADR | Role |
|-----|------|
| [0021](0021-pfd-mfd-cockpit-attention-model.md) | attention model, Dark Cockpit, zone presets |
| [0010](0010-ui-modes-toml-configuration.md) | UI and TOML modes |

### Implementation snapshot

| Element | Meaning |
|---------|----------|
| — | the idea is fixed; contract, download and support - with a separate product initiative |

## Summary

- **CascadeIDE SDK** - stable contracts for internal extension and future plugins.
- Versioning of public APIs; border with “everything internal”.
- Communication with attention zones - [0025](0025-sdk-attention-zones-and-capabilities.md).

---
## Context

The design is now set by **themes** (JSON/application resources) and **presets** modes (`UiModes/*.toml`). The idea of ​​**skins** in the spirit of classic players (for example WinAMP): a package that sets not only the palette, but also the **character of the shell** - density, decorative chrome, optionally other **visual** layout options **within the same semantic anchors**; in the future - exchange of user packages.

For an **open** product this can be a **differentiator**: there are few full-fledged IDEs with such mechanics. At the same time, support is expected: **IDE code is available** - the skin author and the community can disassemble the contract and repair their package; the regular team fixes **invariants** ([0021](0021-pfd-mfd-cockpit-attention-model.md), theme/preset loader), and does not undertake to support each third-party skin as a commercial product.

---

## Solution

1. **Not currently implemented** - separate mechanics for installing, versioning and supporting skins outside the current scope.
2. **When/if** we introduce: the skin is described as a **bundle** with a **manifest** (scheme version, link to the base theme, optional overrides of metrics/design slots), consistent with the theme and preset loader.
3. Skin affects the **visual and density** presentation; **doesn't** replace the attention policy: invariants of **Dark Cockpit**, EICAS/CAS channel and W/C/A priorities ([0021](0021-pfd-mfd-cockpit-attention-model.md) §5–§6) **not** are disabled “for the sake of beauty”.
4. **Semantics of zones** (PFD / MFD / frontal / channels) remains at the level of the product and presets; the skin does not introduce arbitrary new “roles” without passing the model [0021](0021-pfd-mfd-cockpit-attention-model.md).

---

## Consequences

- New interface features are still being designed and tested on the **default theme** and built-in presets; skin - add-on.
- When third-party skins appear, criteria for **availability**, readability and predictability escalation will be needed (§6).
- For **community skins** with an open repository, the main line of support is **documented contract + sources**; parsing and edits on the author/community side reduces the expectation of “fix my package out of the box” at the commercial SLA level.

---

## Open

- Manifest format, package signature/trust, sandbox for community skins.
- Avalonia's limitations and the amount of customization it supports without QA bloat.