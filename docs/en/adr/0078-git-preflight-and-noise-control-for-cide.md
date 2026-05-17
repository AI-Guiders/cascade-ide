<!-- English translation of adr/0078-git-preflight-and-noise-control-for-cide.md. Canonical Russian: ../../adr/0078-git-preflight-and-noise-control-for-cide.md -->

# ADR 0078: Git preflight and noise control of changes in CIDE

**Status:** Accepted · Implemented  
**Date:** 2026-04-20

## Related ADRs

| ADR | Role |
|-----|------|
| [0019](0019-shared-git-core-ide-and-git-mcp.md) | general Git Core |
| [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) | pre-flight as a mandatory step |
| [0077](0077-tech-principles-hub.md) | TECH Principles Center |

---
## Context

In daily work there is repeated operational noise before a commit:

- “phantom” changes to line endings and BOM;
- mixing semantic edits and technical hygiene in one commit;
- unnecessary manual checks before pushing (what exactly goes away, what remains locally, where exactly it is pushed).

This increases cognitive load and makes the story less readable, especially in the human + assistant cycle.

---

## Solution

Introduce into CIDE a minimal built-in loop **Git Preflight**, which is triggered before a commit and gives:

1. **Checking and marking diff noise**  
   Show “EOL/BOM/whitespace only” changes separately, without confusing them with product meaning.
2. **Hygienic auto-fixes with a button**  
   Normalization of line endings by `.gitattributes`, removal of BOM in text files, safe renormalize procedure.
3. **Hint for logical splitting of commits**  
   Drafts of 1..N commits by meaning (for example: `docs`, `tools`, `refactor`) with editable messages.
4. **Post-push health report**  
   A short report: what was pushed, to which remotes, what remained in the working tree.

---

## MVP (iteration 1)

- Add command `Git: Run Preflight`.
- Implement detectors:
  - `eol-only` (line ending normalization only),
  - `bom-only` (removing/adding BOM without semantic diff),
  - `whitespace-only` (spaces/tabs without changing tokens).
- In the preflight window:
  - file grouping `semantic` vs `noise`,
  - action `Apply Safe Fixes` for the noise group,
  - action `Create Logical Commits` (drafts).
- After `push` show a compact report (remote/branch/number of commits/remainder of local changes).

---

## Success Metrics

- Reduced share of “noise-only” commits in history.
- Reducing the proportion of mixed commits (noise + meaning in one).
- Reduced time from `git status` to `git push` in typical tasks.
- Fewer manual rollbacks due to accidental technical changes.

---

## Consequences

- The story becomes more predictable and suitable for review/cherry-pick.
- Reduces friction in pair work “person + assistant”.
- It is necessary to carefully limit auto-fixes so as not to change files without the explicit consent of the user.

---

## Rejected alternatives

- **Leave everything as is (manual Git commands):** flexible, but too much routine and unstable results.
- **Only external hooks/scripts:** useful, but does not provide a seamless UX and situational awareness inside CIDE.