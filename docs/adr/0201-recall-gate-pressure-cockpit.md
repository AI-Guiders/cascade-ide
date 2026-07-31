# ADR 0201: Recall gate on pressure/cockpit (pull → reconcile → align → ready)

**Status:** Accepted  
**Date:** 2026-07-31  
**Tags:** #cdp #continuity #cockpit #pressure #recall

**SSOT implementation:** [CDP-ADR-0024](../../cdp-mcp/docs/adr/CDP-ADR-0024-recall-gate-pull-reconcile-align.md) in `cdp-mcp` (`IdePressureChannel.Gate`, v0.5.324).

## Context

Lifecycle already has `recall`, but agents skipped it under AutoIgnition and continued stale Domain (Avalonia peels while Glass primary). Continuity tools existed; the **decision** step did not.

## Decision (product)

Cockpit-visible recall gate on pressure soft-organ: **pull → reconcile (self-steer) → align → ready**. Reconcile includes internal-locus steer when memo+SSOT suffice. Not a new top-level `CdpPhase`.

## Consequences

Wake/L1 amnesia charge points at gate ops. Glass/Avalonia epic steer lives in TM+stash; gate forces a place to correct before act.

## Non-goals

Hard-block leave-Recall in `SessionContext` (later peel).
