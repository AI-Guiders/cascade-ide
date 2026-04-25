# ADR 0040: LSP (C# / Markdown) — командная строка в `settings.toml`: пресеты, опциональные ключи, переопределение через окружение

**Статус:** Accepted · Implemented (как [§Решение](#решение) ниже)  
**Дата:** 2026-04-13; обновлено 2026-04-25 — канон TOML: `[languages.csharp]` / `[languages.markdown]` + `mode` + вложенные профили  
**Связь:** [0028](0028-user-settings-toml-localappdata-and-secrets.md) (где лежит `settings.toml`, snake_case, модель `CascadeIdeSettings`), [0029](0029-configuration-toml-canonical-ui-facade.md) (TOML как канон; UI — фасад), [0023 environment readiness](0023-environment-readiness-glance.md) (краткие подсказки по LSP без дампа `environ`), [0023 markdown tooling](0023-markdown-diagrams-language-tooling.md) (Markdown как first-class; LSP в долгой перспективе). Реализация: `CSharpLspProviderIds` / `MarkdownLspProviderIds` и `*ResolveForRuntime()` из `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings` (`Models/LanguagesSettings.cs`), хосты `*LspDiagnosticsHost`, строки readiness — `Services/EnvironmentReadinessSnapshotBuilder.cs`.

---

## Контекст

1. В `%LocalAppData%\CascadeIDE\settings.toml` языковые LSP настраиваются в **`[languages.csharp]`** и **`[languages.markdown]`**: дискриминатор **`mode`** (например `ParseOnly` / `OmniSharp` / `Marksman`) и **вложенные таблицы** профилей с **`executable`** / **`arguments`** для stdio.
2. Для встроенных пресетов пользователь часто не дублирует в файле пустые строки «как в примере» — достаточно `mode` и при необходимости переопределений в соответствующей вложенной таблице.
3. Отдельная потребность — **не хранить** абсолютные пути к инструментам в `settings.toml` (общий репозиторий dotfiles, CI, разные машины), но иметь **предсказуемый** способ подставить их из окружения без «магии» в значении `executable`.

---

## Решение

### 1. Канон на сегодня (реализовано): `mode` и вложенные профили

- Разрешение пары **файл процесса + аргументы** берётся из **активного профиля** по `mode` (см. `ResolveForRuntime()` в `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings`).
- Для пресетов с разумным дефолтом на **PATH** пустой или пробель-only **`executable`** в профиле означает: использовать **встроенное имя команды** (например `marksman`, `csharp-ls`, `OmniSharp` с фиксированным префиксом аргументов `--languageserver` — см. код), а не «ошибка конфигурации».
- Пустой **`arguments`** допустим: дополнительные аргументы добавляются к пресету только если пользователь задал непустую строку.
- Профиль **`[languages.csharp.custom]`** (и аналог Markdown) по-прежнему требует непустого **`executable`**, если нет встроенного дефлета для этого пути; это не меняется данным ADR.

### 2. Канон на сегодня: опциональность ключей в TOML

- Ключи **`executable`** и **`arguments`** **не обязаны** присутствовать в вложенной таблице профиля, если устраивают значения по умолчанию из модели (пустые строки) и выбранный `mode` поддерживает пресет из п. 1.
- Пример минимальной конфигурации Markdown LSP:

```toml
[languages.markdown]
mode = "Marksman"
```

(таблица `[languages.markdown.marksman]` с пустыми `executable`/`arguments` — по желанию для явности.)

- Пример с C# (ParseOnly — дефолт, можно не указывать `mode` в файле, если устраивает):

```toml
[languages.csharp]
mode = "ParseOnly"

[languages.csharp.parse_only]
executable = ""
arguments = ""
```

### 3. Предложение на будущее (не обязатель реализовано): явный флаг «из окружения»

**Цель:** дать опцию **не дублировать** путь в TOML и при этом явно сказать IDE «бери из переменных окружения», а не полагаться на случайно пустую строку.

- В **профиле** (или в общем блоке LSP) вводится булево поле, например **`launch_from_environment`** (snake_case в файле, PascalCase в модели — как у остального `settings.toml` по [0028](0028-user-settings-toml-localappdata-and-secrets.md)).
- При **`launch_from_environment = true`**:
  - **`executable`** и **`arguments`** в TOML **могут отсутствовать** или быть пустыми без потери смысла;
  - IDE читает **согласованные** имена переменных (префикс, например `CASCADE_IDE_…`, и суффиксы по виду LSP), документированные в этом ADR и в краткой подсказке readiness ([0023](0023-environment-readiness-glance.md)).
- **Приоритет** после реализации должен быть зафиксирован в коде и здесь же; рекомендуемый порядок: значения из окружения **только если** флаг true; иначе — как сейчас (TOML → пресет). При флаге true и **отсутствующих** переменных — либо откат к пресету как к п. 1 (с явной строкой в readiness), либо «не стартуем» с явной причиной; **не** молчаливое смешивание.
- Имена переменных окружения задаются **отдельным подпунктом** этого ADR при первой реализации (чтобы не блокировать принятие п. 1–2); до реализации статус расширения остаётся **Proposed**.

### 4. Отклонённые альтернативы

- **Магические подстановки** внутри строки `executable` (`$ENV`, `${VAR}` без отдельного флага) — отклонены: хуже объяснять, сложнее валидировать и показывать в readiness.
- **Один глобальный флаг** на оба LSP без привязки к секции — отклонён: путаница с именами переменных и сценариями «только Markdown на CI».

---

## Последствия

- Документация и примеры `settings.toml` сокращают секции LSP до `mode` и при необходимости **одной** вложенной таблицы профиля; полные `executable`/`arguments` остаются валидными для явности.
- При добавлении **`launch_from_environment`** — обновить модели `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings` (и при необходимости обёртки `ResolveForRuntime`), хосты запуска, **Environment readiness** и пример в `docs/samples/settings.toml`; статус расширения в шапке ADR сменить на **Accepted** после ревью.
- [0028](0028-user-settings-toml-localappdata-and-secrets.md) не дублирует семантику LSP — для деталей командной строки и env ссылка **сюда**.

---

## Статус реализации (сверка)

| Часть ADR | Код / артефакты |
|-----------|-----------------|
| Пресеты, пустой `executable` | `CSharpLspProviderIds`, `MarkdownLspProviderIds`, `LanguageServerLaunchProfile` |
| Опциональные ключи в TOML | Десериализация `CascadeIdeSettings` + значения по умолчанию в `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings` |
| `launch_from_environment` | Пока **нет** в модели и резолвере — **Proposed** |
