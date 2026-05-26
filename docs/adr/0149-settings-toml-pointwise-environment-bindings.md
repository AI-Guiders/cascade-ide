# ADR 0149: Точечные привязки `settings.toml` к переменным окружения (`*_env`)

**Статус:** Accepted · Implemented  
**Дата:** 2026-05-24

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `settings.toml`, snake_case, секреты отдельно |
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP: `executable_env` / `arguments_env` в профилях |
| [0023](0023-environment-readiness-glance.md) | readiness: какие env читаются, без дампа `environ` |
| [0144](0144-intercom-team-transport-cide-sync-and-reference-service.md) | `intercom.transport` |

---

## Контекст

Пользовательский `settings.toml` часто копируется между машинами или хранится в общем профиле разработчика. Абсолютные пути к бинарникам (LSP, Intercom service, cursor-agent) не должны быть единственным способом конфигурации. При этом подстановки `$VAR` внутри строк TOML **не** используем — хуже валидировать и показывать в Environment readiness ([0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) §4).

Нужен **явный** парный ключ: литерал в TOML + опциональное имя переменной окружения.

---

## Решение

### 1. Соглашение имён

Для строкового поля `<field>` в TOML добавляется опциональный ключ **`<field>_env`** (snake_case), в модели C# — **`<Field>Env`** (`JsonPropertyName` через общий snake_case сериализатор).

Пример:

```toml
[intercom.transport]
local_server_path = "tools/intercom-service/IntercomService.exe"
local_server_path_env = "CASCADE_INTERCOM_SERVER_EXE"
```

### 2. Приоритет резолва (`SettingsEnvResolver`)

На **чтении** (runtime), не при сохранении файла:

1. Если `<field>_env` — sentinel **`PATH`** (регистронезависимо) → для **запуска процесса** (LSP `executable`, ACP `cursor_acp_path`): эффективный путь **пустой**; дальше пресет + поиск команды в **PATH** ([0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md)). Это **не** чтение переменной окружения `PATH`.
2. Иначе, если `<field>_env` — **имя переменной** и `GetEnvironmentVariable` возвращает непустую строку → использовать её.
3. Иначе → литерал `<field>` из TOML.
4. Дальше по домену — пресеты LSP, legacy defaults, fallback intercom-service и т.д.

Пустой `*_env` = привязки нет (для LSP с пресетом эквивалентно п.1–3 с пустым `executable` и поиском в PATH). Пустая **именованная** переменная **не** перекрывает литерал.

`Environment.ExpandEnvironmentVariables` для `%VAR%` внутри литерала **не** смешиваем с `*_env`; при необходимости — только в отдельных резолверах путей (как MCP JSON path).

### 3. Первая волна полей (v1)

| Секция | Поле | `*_env` | Типичное значение |
|--------|------|---------|-------------------|
| `[languages.*.*]` профиль | `executable` | `executable_env` | **`PATH`** (дефолт для OmniSharp/Marksman) или имя переменной с абсолютным путём |
| то же | `arguments` | `arguments_env` | имя переменной (sentinel `PATH` не используется) |
| `[intercom.transport]` | `local_server_path` | `local_server_path_env` | `CASCADE_INTERCOM_SERVER_EXE` |
| `[intercom.transport]` | `base_url` | `base_url_env` | `CASCADE_INTERCOM_BASE_URL` |
| `[ai.acp]` | `cursor_acp_path` | `cursor_acp_path_env` | `CURSOR_ACP_AGENT_PATH` |
| `[agent_notes]` | `config_path` | `config_path_env` | `CASCADE_AGENT_NOTES_CONFIG` |
| `[agent_notes]` | `kb_base_overlay_path` | `kb_base_overlay_path_env` | `CASCADE_KB_BASE_OVERLAY` |

Секреты (API keys, JWT) — по-прежнему только `*-secrets.toml` / [0028](0028-user-settings-toml-localappdata-and-secrets.md), **не** через `*_env` в основном файле.

Уже существующие process-wide переменные (`AGENT_NOTES_FILE`, `NETCOREDBG_PATH`) сохраняют приоритет там, где код читает их **напрямую**; `config_path_env` дополняет путь из `[agent_notes]`, не заменяет `AGENT_NOTES_FILE`.

### 4. Отклонено

- `launch_from_environment` (глобальный bool) — заменён точечными `*_env` ([0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) §3 обновлён).
- Inline-таблица `{ env = "...", fallback = "..." }` — усложняет десериализацию и UI.
- Универсальный `lookup_environment_variable_name` на все поля `CascadeIdeSettings` — вне scope v1.

---

## Последствия

- `SettingsEnvResolver` — единая точка резолва.
- `ResolveForRuntime()` LSP, `IntercomTransportSettings` helpers, `AgentNotesRuntimeLoader`, `KbBaseOverlayPathResolver`, `CursorAcpAgentPath` получают уже разрешённые строки или вызывают resolver в месте использования.
- Environment readiness: при заданном `*_env` и пустом env — явная подсказка в LSP-строках (имя переменной без значения).
- Расширение на новые поля — новый ADR-подпункт или дополнение таблицы §3.

---

## Статус реализации

| Элемент | Артефакт |
|---------|----------|
| Резолвер | `Services/SettingsEnvResolver.cs` |
| LSP | `LanguageServerLaunchProfile`, [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) |
| Intercom / ACP / agent_notes | модели + call sites |
| Тесты | `SettingsEnvResolverTests`, deserialize samples |
| Дефолты | комментарии в `Settings/defaults-settings.toml` |
