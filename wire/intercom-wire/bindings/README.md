# Language bindings (optional)

Источник истины — `schemas/v1/*.json`. Bindings генерируются или поддерживаются вручную в потребителях:

| Consumer | Подход |
|----------|--------|
| C# (CIDE, intercom-service) | Records в коде; сверка с schema в CI (TODO) |
| TypeScript (intercom-web) | Ручные types → `json-schema-to-typescript` из schemas |

Не дублировать DTO в этом пакете до появления codegen pipeline в CI.
