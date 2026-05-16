# ADR 0110: Рефакторинги Roslyn по диапазону — мост Intent Melody / IDE и Roslyn MCP

**Статус:** Proposed  
**Дата:** 2026-05-11  
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0081](0081-parametric-intent-melodies-editor-line-ranges.md) | параметрический хвост `:start:end`, §3 про рефакторинги |
| [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) | каталог `[[melody_root]]`, сборка args в коде |
| [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md) | сопряжение агента с Roslyn MCP |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id` |

### Вне ADR

| Документ | Роль |
|----------|------|
| [roslyn-mcp](../../../roslyn-mcp/README.md) | отдельный MCP-сервер |

## Контекст

Полноценные рефакторинги C# (Extract Method, Extract Interface и т.д.) в экосистеме Cascade реализованы **в процессе Roslyn MCP**: `roslyn_get_code_actions` и `roslyn_apply_code_action` с опциональным **диапазоном** (`end_line`, `end_column`) — см. схемы тулов и `ServiceLayer/CodeActions.cs` в репозитории **roslyn-mcp**.

В **ядре CascadeIDE** не дублируется стек **Microsoft.CodeAnalysis.CSharp.Features** и MSBuildWorkspace для тех же операций: in-process редактор использует упрощённую семантику ([`CSharpLanguageService`](../../Services/CSharp/CSharpLanguageService.cs)) без полноценного конвейера code actions.

Ранее обсуждались mnemonic вида `rmx` / `rix` ([0081](0081-parametric-intent-melodies-editor-line-ranges.md)); в коде **не хранить** заглушки без реального `command_id` — это рассинхрон с каталогом и палитрой.

---

## Проблема

1. Пользователь ожидает **одну строку** `c:…` или **IdeCommands**, ведущую к тому же результату, что ручной вызов Roslyn MCP.
2. Дублировать реализацию рефакторингов внутри **CascadeIDE.exe** — дорого и расходится с единственным источником правды в **roslyn-mcp**.
3. Нужно явное **архитектурное место** для будущего решения (мост, делегирование, настройки), без фиктивных записей в TOML.

---

## Решение (направление)

1. **Канон операций по диапазону в ядре IDE** на сегодня: уже реализованные слои — **выделить** (`select`), **заменить текст** (`apply_edit`), URL-портал и т.д. через каталог [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md).
2. **Extract Method / Extract Interface и аналоги** до отдельного ADR **Accepted** не объявлять обязательными slug’ами в [`intent-melody-aliases.toml`](../../IntentMelody/intent-melody-aliases.toml). Варианты следующей итерации (взаимоисключающие или комбинируемые — решить при реализации):
   - **Агент / внешний хост** вызывает **Roslyn MCP** с тем же solution/project path и диапазоном после того, как IDE выставила выделение (`c:els:…` или эквивалент).
   - **Опциональный мост** в IDE: конфигурируемый путь (localhost MCP, stdio, будущий in-proc host) и тонкий `command_id`, который сериализует intent + диапазон и делегирует **roslyn-mcp** — см. [0058](0058-agent-roslyn-mcp-coupling-settings-toml.md).
   - **Отказ от отдельных мнемоник** `rmx`/`rix` в пользу документированного сценария «выделить диапазон → code actions в Roslyn MCP».

3. Заглушки каталога **без** `command_id` в бандле **не использовать** — нарушают инвариант [0109](0109-declarative-parametric-melody-catalog-toml-and-code-binders.md) (исполнение через `command_id` + args).

---

## Последствия

- Документ **0081** ссылкой на этот ADR фиксирует границу: **el\*** — в зоне продукта ядра; **r\*** и Roslyn-рефакторинги — **после** явного моста или только через внешний MCP.
- Реализация моста — отдельные коммиты: контракт args, настройки, тесты, при необходимости новые `IdeCommands` и регенерация ProtocolDocGen.

---

## Отклонённые альтернативы (кратко)

| Альтернатива | Почему не сейчас |
|--------------|------------------|
| Встроить **CSharp.Features** + MSBuildWorkspace целиком в CascadeIDE | Дублирование **roslyn-mcp**, тяжёлый конвейер, размер деплоя |
| Оставить **rmx**/**rix** в TOML без исполнения | Путает палитру и аккорд ([0060](0060-keyboard-chord-stack-fms-tactical-strategic.md)) |
