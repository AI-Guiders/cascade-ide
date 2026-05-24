# Intercom Wire (open protocol)

**Канон протокола** Cascade Intercom — в репозитории **cascade-ide** (`wire/intercom-wire/`). Логически отделён от runtime сервера и UI IDE.

| Слой | Что это | Где |
|------|---------|-----|
| **Wire** | События, payload, extensions (`relates_to`, code/doc) | `schemas/` |
| **Transport profile** | Как доставить wire (HTTP+SSE, Matrix, …) | `profiles/` |
| **Bindings** | DTO/codegen для языков | `bindings/` (опционально) |

Политика: [ADR 0142](../../docs/adr/0142-intercom-open-wire-pluggable-transports.md) (Accepted).  
Governance: [ADR 0146](../../docs/adr/0146-intercom-wire-canonical-protocol-package.md) (Accepted).  
Reference server: [intercom-service](../../host/intercom-service/README.md) — profile `reference-http-v1`.

## Версионирование

| Уровень | Поле / путь | Правило |
|---------|-------------|---------|
| **Wire major** | `transport-envelope.schema_version` | `1` — breaking меняет major |
| **Wire minor** | `CHANGELOG.md`, extensions | additive: новые `event_kind`, optional поля, `extensions/*` |
| **HTTP profile** | `/api/v1/…` | новый profile → `profiles/reference-http-v2/` |

**Совместимость (норма):**

- Клиенты и серверы **MUST** сохранять неизвестные поля JSON (не отбрасывать).
- Новые `event_kind` — сначала PR в **wire/** , потом реализации.
- Extensions (`relates_to`, `code_doc_link`) — optional на message payload; старые клиенты игнорируют.

## Структура `schemas/v1/`

```
schemas/v1/
  transport-envelope.schema.json
  event-kinds.json
  extension-registry.json
  common/attachment-anchor.schema.json
  payloads/*.schema.json
  extensions/*.schema.json
  team-manifest.schema.json
```

## Потребители

| Потребитель | Репозиторий | Использование |
|-------------|-------------|----------------|
| [host/intercom-service](../../host/intercom-service/) | **cascade-ide** | HTTP+SSE profile |
| [Features/Intercom/Transport](../../Features/Intercom/Transport/) | **cascade-ide** | CIDE FederatedSync |
| **intercom-web** | **[AI-Guiders/intercom-web](https://github.com/AI-Guiders/intercom-web)** (MIT) | PWA; wire — submodule/copy/raw URL |
| [docs/intercom-wire](../../docs/intercom-wire/README.md) | cascade-ide | Указатель из docs |

## Разработка расширений

1. JSON Schema в `schemas/v1/extensions/`.
2. `extension-registry.json` + при необходимости `event-kinds.json`.
3. `CHANGELOG.md` (minor).
4. Service / CIDE / web — отдельные PR в том же репо.

Не смешивать breaking wire и breaking HTTP profile без version bump.
