# Transport profiles

**Profile** = binding wire к конкретному API (HTTP, Matrix CS API, …).  
Меняется **без** смены `schemas/v1` payload, если mapping 1:1.

| Profile | Статус | Реализация |
|---------|--------|------------|
| [reference-http-v1](reference-http-v1/) | Reference | [intercom-service](../../../host/intercom-service/) |

Новый мост (Matrix, Mattermost) → новая папка `profiles/<name>-v1/` + ADR моста; wire остаётся в `schemas/`.
