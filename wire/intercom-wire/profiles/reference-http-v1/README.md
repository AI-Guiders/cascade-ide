# Reference HTTP transport profile v1

OpenAPI описывает **доставку** envelope из `schemas/v1/transport-envelope.schema.json`.

- Не дублирует семантику payload — см. `schemas/v1/payloads/*`.
- Версия URL: `/api/v1` (profile version, не wire `schema_version`).

Реализация: [intercom-service](../../../../host/intercom-service/).
