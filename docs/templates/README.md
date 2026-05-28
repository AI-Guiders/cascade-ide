# Documentation templates

These templates are meant to be **copied** into a concrete doc (ADR / feature doc / module doc) and then filled in.

They work together with doc correspondence (ADR 0061 / 0155):
- if a file/feature has **no docs**, the UI can suggest a template to start with;
- an agent can be asked to “fill template X for feature Y” using the correspondence scope.

## Templates

- `feature.md`: feature-level documentation (scope, UX, invariants, knobs, links)
- `module.md`: subsystem/module overview (contracts, responsibilities, dependencies)
- `adr-mini.md`: small ADR skeleton (problem → decision → consequences)
- `runbook.md`: operational runbook (how to debug/operate)

