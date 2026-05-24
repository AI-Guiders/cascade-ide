# ADR 0146: Intercom Wire — канонический пакет протокола

| Поле | Значение |
|------|----------|
| **Статус** | Accepted |
| **Дата** | 2026-05-24 |
| **Amend** | 2026-05-24 — wire + reference service в репозитории **cascade-ide** |
| **Связь** | [0142](0142-intercom-open-wire-pluggable-transports.md) · [0144](0144-intercom-team-transport-cide-sync-and-reference-service.md) · [0145](0145-intercom-web-pwa-team-client.md) |

## Резюме

Протокол Intercom — **`wire/intercom-wire/`** в репозитории **cascade-ide**. Развивается **логически** отдельно от `host/intercom-service` и кода IDE (отдельные PR), но **один git clone** для команды.

## Контекст

Расширения (`relates_to`, code↔doc, …) и новые `event_kind` не должны требовать синхронного PR в сервер. Transport сильно связан с CIDE — отдельный git в `Financial/software/open/` создавал лишние slnx и пути `../../../`.

## Решение

| Артефакт | Путь (в cascade-ide) |
|----------|-------------------------|
| **Wire schemas** | `wire/intercom-wire/schemas/v1/` |
| **Extension registry** | `extension-registry.json`, `extensions/*` |
| **Event kind registry** | `event-kinds.json` |
| **HTTP profile** | `wire/intercom-wire/profiles/reference-http-v1/openapi.yaml` |
| **Reference server** | `host/intercom-service/` |
| **Указатель из docs** | `docs/intercom-wire/README.md` |
| **Solution** | `CascadeIDE.slnx` — IDE + IntercomService + Tests |
| **Путь сервера** | `host/intercom-service/` (не `services/` — коллизия с `Services/` на Windows) |

### Правила эволюции

1. **Additive** — optional поля, новые extensions, новые kind → wire minor (`CHANGELOG.md`).
2. **Breaking** — `transport-envelope.schema_version` → `2`.
3. **HTTP** — новый profile или `/api/v2`; wire может остаться v1.
4. Реализации **MAY** отставать: unknown JSON сохраняется.

### relatesTo / code-doc

- `extensions/relates-to-v1` — optional `relates_to[]` на message.
- `extensions/code-doc-link-v1` — `code_doc_links[]`.
- `message_range_related` — gutter ordinals ([0137](0137-intercom-message-code-correspondence.md)).

## Последствия

- Один репозиторий для пилота; PR wire vs service по смыслу раздельны.
- Matrix/MM bridges могут копировать только `wire/` или submodule позже.
- CI (будущее): validate schemas + OpenAPI в pipeline cascade-ide.

## Потребители вне репо

| Клиент | Репозиторий | Wire |
|--------|-------------|------|
| **intercom-web** | Org GitHub, отдельно от cascade-ide ([0145](0145-intercom-web-pwa-team-client.md)) | копия/submodule `wire/intercom-wire` или raw GitHub |

## Не цели

- NuGet / npm publish wire (пока path в cascade-ide).
- intercom-web **внутри** cascade-ide (отдельный org-репо — норма).
- CloudEvents mandatory envelope.
