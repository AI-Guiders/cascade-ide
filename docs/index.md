# Cascade IDE — документация

**Cascade IDE (CIDE)** — agent-first IDE на **.NET** и **Avalonia**: in-proc MCP, модель внимания кокпита (PFD / Forward / MFD), канал **Intercom**.

## С чего начать

| Раздел | Описание |
|--------|----------|
| [Навигатор ADR по статусам](site/adr-nav/index.md) | Proposed, Accepted, Implemented, Superseded — автообновление из шапок ADR |
| [Полный индекс ADR](adr/README.md) | Таблица всех решений и тематические кластеры |
| [Жизненный цикл статусов ADR](adr/status-lifecycle.md) | Как читать `Proposed` / `Accepted · Implemented` |
| [Текущая архитектура](architecture/current-architecture-v1.md) | Снимок реализации |
| [MCP-протокол IDE](MCP-PROTOCOL.md) | Команды для агента и человека |
| [Раскладка UI (Flight)](ui-ux/cascade-ide-ui-layout-v1.md) | PFD · Forward · MFD, имена контролов для MCP |
| [Карта концепт → код](ui-ux/concept-to-implementation-map-v1.md) | Что в коде vs архивные концепты |

## Репозиторий

- Исходники: [github.com/AI-Guiders/cascade-ide](https://github.com/AI-Guiders/cascade-ide)
- Организация: [AI-Guiders](https://ai-guiders.github.io/)
- Лицензия кода: MIT · коммерческие вопросы — [COMMERCIAL-NOTICE.md](COMMERCIAL-NOTICE.md)

!!! note "Язык"
    Тела ADR — **на русском** (`docs/adr/`). Для англоязычных читателей: переключатель **EN** в шапке → [Concept overview](en/concept-overview.md), полный каталог [ADR (EN)](en/adr/README.md), [раскладка UI (EN)](en/ui-ux/cascade-ide-ui-layout-v1.md).
