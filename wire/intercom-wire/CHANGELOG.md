# Changelog — Intercom Wire

Формат по [Keep a Changelog](https://keepachangelog.com/). Версия пакета = wire **minor** (schema_version envelope остаётся `1` пока нет breaking).

## [1.1.0] - 2026-05-24

### Added

- Канонический пакет `intercom-wire` (вынесен из `cascade-ide/docs/intercom-wire`).

### Changed

- Перенос в репозиторий **cascade-ide**: `wire/intercom-wire/`, сервер `host/intercom-service/`, один `CascadeIDE.slnx`.
- `extensions/relates-to-v1` — optional `relates_to[]` на message (связь с кодом/сообщением/doc).
- `extensions/code-doc-link-v1` — связка KB/doc ↔ code anchor.
- `payloads/message-range-related` — gutter ordinals ↔ code (ADR 0137).
- `extension-registry.json`, `event-kinds.json`.
- Profile `reference-http-v1` OpenAPI (reference binding, не протокол).

### Notes

- `intercom-service`, CIDE, `intercom-web` потребляют схемы отсюда; DTO в коде догоняют отдельными PR.

## [1.0.0] - 2026-05-24

### Added

- Transport envelope v1, message / thread / edit payloads.
- `attachment-anchor` common type ([ADR 0128](../cascade-ide/docs/adr/0128-intercom-attachment-anchors-and-code-references.md)).
- `team-manifest` для `.cascade-ide/intercom-team.toml`.
