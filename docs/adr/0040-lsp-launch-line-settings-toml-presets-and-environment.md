# ADR 0040: LSP (C# / Markdown) — командная строка в `settings.toml`: пресеты, опциональные ключи, переопределение через окружение

**Статус:** Accepted · Implemented  
**Дата:** 2026-04-13; обновлено 2026-05-24 — `executable_env` / `arguments_env` ([0149](0149-settings-toml-pointwise-environment-bindings.md))

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | где лежит `settings.toml`, snake_case, модель `CascadeIdeSettings` |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | TOML как канон; UI — фасад |
| [0023](0023-environment-readiness-glance.md) | краткие подсказки по LSP без дампа `environ` |
| [0149](0149-settings-toml-pointwise-environment-bindings.md) | общее соглашение `*_env` и приоритет резолва |
| [0023](0023-markdown-diagrams-language-tooling.md) | Markdown как first-class; LSP в долгой перспективе |

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

### 3. Точечные привязки к окружению (реализовано, [0149](0149-settings-toml-pointwise-environment-bindings.md))

В каждом **профиле** LSP опциональны пары:

- **`executable_env`** — **`PATH`** (явно: команда пресета ищется в PATH) **или** имя переменной с абсолютным путём;
- **`arguments_env`** — только имя переменной с аргументами (без sentinel `PATH`).

Пример (намерение видно в файле):

```toml
[languages.csharp]
mode = "OmniSharp"

[languages.csharp.omni_sharp]
executable = ""
executable_env = "PATH"
arguments = ""
```

Резолв: `ResolveLaunchPath` → при `PATH` пустой `executable` → пресет `CSharpLspProviderIds` (п. 1). Именованная переменная — когда бинарник не в PATH ([0149](0149-settings-toml-pointwise-environment-bindings.md) §2). Отклонён **`launch_from_environment`** (глобальный bool).

### 4. Отклонённые альтернативы

- **Магические подстановки** внутри строки `executable` (`$ENV`, `${VAR}` без отдельного флага) — отклонены: хуже объяснять, сложнее валидировать и показывать в readiness.
- **Один глобальный флаг** на оба LSP без привязки к секции — отклонён: путаница с именами переменных и сценариями «только Markdown на CI».

---

## Последствия

- Документация и примеры `settings.toml` сокращают секции LSP до `mode` и при необходимости **одной** вложенной таблицы профиля; полные `executable`/`arguments` остаются валидными для явности.
- `*_env` — см. [0149](0149-settings-toml-pointwise-environment-bindings.md); LSP-специфика — этот ADR §3.
- [0028](0028-user-settings-toml-localappdata-and-secrets.md) не дублирует семантику LSP — для деталей командной строки и env ссылка **сюда**.

---

## Статус реализации (сверка)

| Часть ADR | Код / артефакты |
|-----------|-----------------|
| Пресеты, пустой `executable` | `CSharpLspProviderIds`, `MarkdownLspProviderIds`, `LanguageServerLaunchProfile` |
| Опциональные ключи в TOML | Десериализация `CascadeIdeSettings` + значения по умолчанию в `CSharpLanguageServerSettings` / `MarkdownLanguageServerSettings` |
| `executable_env` / `arguments_env` | `LanguageServerLaunchProfile`, `SettingsEnvResolver`, [0149](0149-settings-toml-pointwise-environment-bindings.md) |
