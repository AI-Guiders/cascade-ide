<!-- English translation of adr/0077-tech-principles-hub.md. Canonical Russian: ../../adr/0077-tech-principles-hub.md -->

# ADR 0077: TECH - the center of principles (connected text from the canon)

**Status:** Proposed  
**Date:** 2026-04-20  

**Purpose:** one **input** ADR for the reader: **how we think about technology** - module boundaries, contracts with the outside world, agent, debugging and repository infrastructure - without going through dozens of records. Details and exception tables are in the original ADR and in [architecture-policy.md](../../architecture-policy.md).

**Text canon** below - files in [`snippets/tech/`](snippets/tech/README.md); wording changes are made there, this ADR sets the structure and **Proposed/Accepted** status for the TECH “center”.

## Related ADRs

| ADR | Role |
|-----|------|
| [0006](0006-presentation-layers-and-feature-slices.md) | layers and slices |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | Stable MCP contracts and testable infrastructure |

### Outside ADR

| Document | Role |
|----------|------|
| [TECH/README.md](TECH/README.md) | pointer `TECH/` |
| [TECH/principles.md](TECH/principles.md) | TECH/principles |
**Build:** in GitHub raw `{{ INCLUDE }}` are not expanded - for reading “like a book”: `dotnet script build-adr.csx` (expanded `0077` will go into the general workbook) or collect HTML from the root `docs/adr` point by point after including 0077 in your `adr-book.md`.

---

## Introduction

The goal is **not “all in one monolith”** and not “everyone on their own”: **consistent boundaries** between UI, scripts and the outside world, plus a **fair** outline for human and agent (debugging, transport, contracts). Below are two blocks: **contracts and infrastructure**, then **agent, debugging and observability**.

{{ INCLUDE: snippets/tech/0077-boundaries-contracts-and-infra.md }}

{{ INCLUDE: snippets/tech/0077-agent-debug-and-observability.md }}

---

## Consequences

- Onboarding and reviews can link **"start with [0077](0077-tech-principles-hub.md)"**, then follow the links to full ADRs.
- Expansion of the “center” - new sections in `snippets/tech/` + new `INCLUDE` here; There is no need to duplicate long text in several ADRs unnecessarily.

---

## Rejected alternatives

- **Table of references only** - not enough for a reader who wants **one coherent text** (see [TECH/principles.md](TECH/principles.md) as a map, not as a replacement for this ADR).
- **Duplicate the entire standard** from 0008/0002 in this file - out of sync; canon remains in the original ADR+ snippets.