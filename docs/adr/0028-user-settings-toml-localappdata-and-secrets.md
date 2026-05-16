# ADR 0028: Пользовательские настройки — `settings.toml`, каталог `%LocalAppData%\CascadeIDE\`, секреты отдельно

**Статус:** Accepted · Implemented  
**Дата:** 2026-04-08  
**Обновлено:** 2026-04-13 — LSP-пресеты в `settings.toml` ([0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md)). Подробности — [§ История](#adr0028-history).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0010](0010-ui-modes-toml-configuration.md) | TOML для **бандла режимов** и **репозиторного** `workspace.toml` — другой слой, не путать с пользовательским файлом |
| [0013](0013-command-surface-and-discoverability.md) | `hotkeys.toml` рядом с `settings.toml` — задумано, не обязательно реализовано |
| [0026](0026-markdown-preview-surfaces-and-placement.md) | часть пресетов в **merged** `workspace.toml`; пользовательский override размещения — не только `settings.toml` |
| [0027](0027-small-team-focus-vs-public-maturity.md) | предсказуемые пути конфигурации |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML как канон; UI — фасад над тем же файлом |
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | `presentation` — прежде всего **`settings.toml`**, не репо |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | C#/Markdown LSP: пресеты, опциональные ключи `executable`/`arguments`, опционально окружение |

### Снимок реализации

| Элемент | Значение |
|---------|----------|
| Сервис | `SettingsService` |
| Каталог | `%LocalAppData%\CascadeIDE\` |
| Настройки | `settings.toml` (Tomlyn, snake_case) |
| Секреты | `ai-keys.toml` (отдельно от настроек) |

## Резюме

- Пользовательский канон: **`%LocalAppData%\CascadeIDE\settings.toml`** (Tomlyn).
- Секреты — **`ai-keys.toml`**, не в основном файле настроек.
- Отдельно от бандла `UiModes/` и merge `.cascade/workspace.toml` ([0010](0010-ui-modes-toml-configuration.md)).
- LSP-пресеты — [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md).

---
## Контекст

В продукте уже есть **несколько слоёв** конфигурации: шипнутый бандл `UiModes/`, merge с `.cascade/workspace.toml` в открытом репозитории ([0010](0010-ui-modes-toml-configuration.md)), глобальные пользовательские предпочтения (AI, MCP, LSP, видимость панелей, локаль UI и т.д.). При этом **отдельного ADR**, который фиксирует **именно пользовательский** канал (путь на диске, формат, что куда кладём), не было — ссылки размазаны по [0010](0010-ui-modes-toml-configuration.md), [0015](0015-editor-toml-syntax-highlighting.md), UX-докам.

Нужна **одна точка канона**: где лежит «мой компьютер», что в TOML, что нельзя класть в TOML, как соотносится с репозиторным слоем.

---

## Решение

### 1. Каталог пользовательских данных CascadeIDE

- **Базовый путь:** `%LocalAppData%\CascadeIDE\` (через `Environment.SpecialFolder.LocalApplicationData` + сегмент `CascadeIDE`).
- Каталог **создаётся** при первом обращении, если отсутствует (`SettingsService.GetSettingsDirectory()`).
- Этот каталог — **не** каталог решения и **не** `AppContext.BaseDirectory`; он **привязан к пользователю Windows** и установке приложения на машине.

### 2. Основной файл настроек: `settings.toml`

- **Полный путь:** `%LocalAppData%\CascadeIDE\settings.toml`.
- **Модель:** `CascadeIdeSettings` — единственный источник правды по **набору полей** и смыслам (AI-провайдеры, MCP, LSP C#/Markdown, Kroki, видимость панелей, `UiMode`, локаль UI и т.д.).
- **Сериализация:** Tomlyn через `CascadeTomlSerializer`: ключи в файле — **snake_case**, свойства C# — PascalCase (`TomlSerializerOptions` с `JsonNamingPolicy.SnakeCaseLower`).
- **Загрузка:** при старте `SettingsService.Load()`; при ошибке чтения/парсинга — **модель по умолчанию** из кода, без падения IDE.
- **Сохранение:** `SettingsService.Save(CascadeIdeSettings)` — перезапись целого файла; ошибки записи **глотаются** (текущая политика реализации — не блокировать UI).

**До публичного релиза:** не наращивать в `SettingsService` автоматические миграции при переименовании/переносе ключей TOML — см. подраздел **«До публичного релиза»** в [корневом README](../../README.md#до-публичного-релиза). После появления массовых установок — отдельное решение (версия файла, одноразовый мигратор, changelog).

### 3. Исторически: JSON больше не поддерживается

- Ранее в коде была однократная миграция **`settings.json` → `settings.toml`**; по состоянию на принятие этого ADR **поддерживаемых старых установок нет**, ветка миграции **удалена** из `SettingsService.Load()`.
- **Канон** — только **`settings.toml`**; при отсутствии файла — значения по умолчанию из кода. Пользовательский **`settings.json`** в `%LocalAppData%\CascadeIDE\` **не** читается (если файл остался вручную — его нужно перенести в TOML самостоятельно или удалить).

### 4. Секреты: не в `settings.toml`

- **API-ключи** (Anthropic, OpenAI, DeepSeek и т.п.) хранятся в **`%LocalAppData%\CascadeIDE\ai-keys.toml`**, отдельная модель `AiKeys`, сериализация через **`CascadeTomlSerializer`** (как у `settings.toml`: ключи в файле — **snake_case**), `AiKeysStorage.Load` / `Save`.
- **Причины разделения:** не светить секреты в том же файле, что удобно копировать/показывать в логах; проще политика бэкапа и `.gitignore` для пользователя; соответствует направлению [0013](0013-command-surface-and-discoverability.md) (не смешивать всё в одном «комке»). Формат **TOML**, а не отдельный JSON — один стек сериализации с пользовательскими настройками.
- **Не коммитить** `ai-keys.toml`; в документации для разработчиков предполагается только локальная машина. Файл **`ai-keys.json`** не читается (если остался руками — перенести в TOML или удалить).

### 5. Что этот ADR *не* описывает (явное разграничение с [0010](0010-ui-modes-toml-configuration.md))

| Слой | Где | Назначение |
|------|-----|------------|
| Пользователь | `%LocalAppData%\CascadeIDE\settings.toml` | Предпочтения **пользователя** на этой машине (модель `CascadeIdeSettings`). |
| Бандл приложения | `UiModes/` рядом с exe | Режимы UI, индекс, **шипнутый** `workspace.toml` бандла. |
| Репозиторий | `<solution>/.cascade/workspace.toml` | Overlay команды/проекта поверх бандла (merge в [0010](0010-ui-modes-toml-configuration.md)). |

#### Имя `workspace.toml`: один контракт merge, не «два смысла» — и где файла нет

- **Хром и пресеты режимов ([0010](0010-ui-modes-toml-configuration.md)):** одна модель данных (`UiWorkspaceToml`), два **источника** одного имени файла — шипнутый **`UiModes/workspace.toml`** и при открытом решении опционально **`<repo>/.cascade/workspace.toml`**. Это **цепочка merge** (бандл → overlay репо), а не два разных продукта с одним именем.
- **`%LocalAppData%\CascadeIDE\`:** файла **`workspace.toml` нет** и в текущем каноне **не задуман** — глобальные пользовательские настройки живут в **`settings.toml`**. Типичная путаница при чтении доков: искать «user workspace» рядом с `settings.toml` под тем же именем.

**Переименование** `workspace.toml` в бандле/репо (например в `ui-workspace.toml`) **не делаем** только ради уникальности имени на диске: затронет сборку, merge, ключи в [0026](0026-markdown-preview-surfaces-and-placement.md), примеры и ожидания «workspace = раскладка IDE». Если когда-то понадобится — **отдельный ADR** + миграция путей и версии бандла.

Состояние, привязанное к **конкретному решению** (например выбранный стартовый проект для отладки: `Services/StartupProjectStore` → `<каталог .sln>/.cascade-ide/startup-project.json`), **не** лежит в `%LocalAppData%\CascadeIDE\` — это отдельный канал «рядом с репо», не глобальные пользовательские настройки.

### 6. Будущие файлы в том же каталоге

- **`hotkeys.toml`** — по намерению [0013](0013-command-surface-and-discoverability.md), рядом с `settings.toml`, только переопределения; до реализации этот ADR не обязует их наличие.
- Раскладка по **физическим дисплеям** (`presentation` / `zone_screen_layout` и токены грамматики по [0017](0017-multi-window-workspace-and-agent-surfaces.md)) — **персональный** слой, **прежде всего** поля в **`settings.toml`**, а не обязательство команды через репозиторный `workspace.toml` ([0017 § слой хранения](0017-multi-window-workspace-and-agent-surfaces.md#adr0017-presentation-grammar)).
- Дополнительные пользовательские файлы (например состояние окон по [0017](0017-multi-window-workspace-and-agent-surfaces.md)) — **отдельным ADR** при появлении договорённости, с явной привязкой к этому каталогу или к новому ключу в `settings.toml`.

---

## Последствия

- Документация и агенты: при слове «настройки пользователя» — путь к **`settings.toml`** и модель **`CascadeIdeSettings`**; при «секретах API» — **`ai-keys.toml`**.
- Новые поля в пользовательских настройках добавляются в **`CascadeIdeSettings`** + при необходимости UI/сохранение; при смене смысла слоя — **обновлять этот ADR** или ссылаться на новый.
- Тесты могут подменять загрузку через существующие механизмы или экземпляры модели без записи на диск — без изменения канона путей.

---

## Отклонённые альтернативы

- **Держать пользовательские настройки в JSON как основной формат** — отклонено: канон — **TOML**; автоматическая миграция с `settings.json` **снята** с кода при отсутствии поддерживаемых legacy-профилей.
- **Хранить API-ключи в `settings.toml`** — отклонено: смешение секретов с переносимым/редактируемым конфигом и риск утечки при обмене файлами.
- **Держать секреты в отдельном JSON (`ai-keys.json`)** — отклонено в пользу **`ai-keys.toml`**: единый формат с `settings.toml` и тот же `CascadeTomlSerializer`; отдельный файл по-прежнему изолирует секреты от «обычного» конфига.
- **Один ADR на все TOML** — отклонено: [0010](0010-ui-modes-toml-configuration.md) остаётся про режимы и merge; пользовательский файл — отдельный контракт пути и содержимого.
- **Переименовать `workspace.toml` в бандле/репо только из‑за совпадения имени с ожиданиями** — отклонено без отдельного ADR и миграции (см. §5.1).

---

## История изменений

<a id="adr0028-history"></a>

| Дата | Изменение |
|------|-----------|
| 2026-04-08 | Канон: `settings.toml`, каталог LocalAppData; `workspace.toml` в merge-слое ([0010](0010-ui-modes-toml-configuration.md)). |
| 2026-04-08 | Удалена миграция `settings.json` → TOML; legacy не поддерживается. |
| 2026-04-08 | Секреты: `ai-keys.toml` вместо JSON; миграции с JSON нет. |
| 2026-04-11 | До публичного релиза — без auto-migrate схемы ([README § До публичного релиза](../../README.md#до-публичного-релиза)). |
| 2026-04-11 | `presentation` → [0017](0017-multi-window-workspace-and-agent-surfaces.md). |
| 2026-04-13 | LSP в `settings.toml` → [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md). |
