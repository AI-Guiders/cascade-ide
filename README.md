# CascadeIDE (CIDE)

**CascadeIDE** — десктопная IDE на **Avalonia** / **.NET 10** для работы с решениями **.sln / .slnx**, с **встроенным MCP-сервером** (агент может дергать сборку, тесты, UI, Git и т.д. через стандартный MCP-транспорт) и панелями **чат / модели** (в т.ч. локальный **Ollama**).

Репозиторий: [github.com/KarataevDmitry/cascade-ide](https://github.com/KarataevDmitry/cascade-ide). Лицензия кода: **[MIT](LICENSE)**.

---

## Быстрый старт

**Нужно:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), ОС с поддержкой Avalonia (Windows / Linux / macOS). Для чата с локальной моделью — запущенный [Ollama](https://ollama.com) (`http://localhost:11434`).

```bash
git clone https://github.com/KarataevDmitry/cascade-ide.git
cd cascade-ide
dotnet restore
dotnet build CascadeIDE.sln -c Release
dotnet run --project CascadeIDE.csproj
```

При старте приложение проверяет Ollama и подскажет, если API недоступен.

**Быстрая итерация (без тяжёлых шагов генерации):**

```bash
dotnet build CascadeIDE.sln -c Debug -p:FastBuild=true
dotnet run --project CascadeIDE.csproj -p:FastBuild=true
```

---

## Что уже есть (коротко)

| Область | Суть |
|--------|------|
| Редактор | AvalonEdit + TextMate (в т.ч. C#), правки из MCP; пакеты **AIGuiders.AvaloniaEdit** с [форка](https://github.com/KarataevDmitry/AvaloniaEdit) |
| Решение | Дерево `.sln` / `.slnx`, открытие файлов |
| Чат | Ollama / OpenAI / Anthropic / DeepSeek, стриминг |
| MCP IDE | `--mcp-stdio` — инструменты вроде `ide_build`, `ide_run_tests`, Git, UI; см. **[docs/MCP-PROTOCOL.md](docs/MCP-PROTOCOL.md)** |
| Отладка | Связка с **dotnet-debug-mcp**, см. **[docs/debug-human-agent-parity-v1.md](docs/debug-human-agent-parity-v1.md)** |
| Git | Вкладка Git: статус, diff, submodule, коммит/push — **[docs/git-and-submodules-v1.md](docs/git-and-submodules-v1.md)** |
| UI-режимы | Focus / Balanced / Power (`Alt+1` … `Ctrl+Alt+M` — см. хоткеи в приложении) |
| Настройки | `%LocalAppData%\CascadeIDE\settings.toml`, данные — WitDatabase рядом |

Источник списка MCP-инструментов в коде: `Services/IdeMcpToolCatalog*.cs`; человекочитаемая таблица — в **MCP-PROTOCOL** (блок `GENERATED:IdeCommands` обновляется из `tools/CascadeIDE.ProtocolDocGen`).

---

## Документация в репозитории

| Документ | Зачем |
|----------|--------|
| [docs/architecture-policy.md](docs/architecture-policy.md) | Слои, срезы, правила изменений |
| [docs/architecture-migration.md](docs/architecture-migration.md) | Карта выноса фич из «кома» |
| [Features/README.md](Features/README.md) | Оглавление каталога `Features/` |
| [docs/MCP-PROTOCOL.md](docs/MCP-PROTOCOL.md) | Контракт MCP IDE |
| [docs/ux/README.md](docs/ux/README.md) | UX: раскладка, имена зон для автоматизации |
| [docs/design/north-star-cursor-mcp-cascade-workbench-v1.md](docs/design/north-star-cursor-mcp-cascade-workbench-v1.md) | Северная звезда продукта |
| [SETUP.md](SETUP.md) | Чеклист окружения (SDK, шаблоны Avalonia, Ollama, GPU) |
| [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md) | Сторонний код и вендоры |
| [docs/COMMERCIAL-NOTICE.md](docs/COMMERCIAL-NOTICE.md) | Коммерческое предложение / контакты |
| [docs/cursor-rules-examples.md](docs/cursor-rules-examples.md) | Примеры `.cursor/rules` для копипаста |
| [docs/backlog-ideas-from-doc-pipeline-v1.md](docs/backlog-ideas-from-doc-pipeline-v1.md) | Идеи вне текущего спринта (документный пайплайн и др.) |

**ACP (Agent Client Protocol):** клиентский SDK вендорится в **`externals/acp-csharp/`** и подключается **ProjectReference** (см. [ADR 0016](docs/adr/0016-agent-client-protocol-external-agent.md)); smoke на .NET — **`samples/AcpSmokeDotnet`**.

---

## Стек (ориентиры)

- **.NET 10**, **Avalonia 12.x**, **CommunityToolkit.Mvvm**
- **Roslyn** — анализ и подсказки вокруг C#
- **Ollama** + расширения Microsoft для AI — чат

Детали установки тулчейна на машину — **[SETUP.md](SETUP.md)**.

---

## Submodule в монорепе **financial-open**

CascadeIDE часто подключают как субмодуль из репо **open** / **financial-open**:

```bash
git submodule add https://github.com/KarataevDmitry/cascade-ide.git cascade-ide
git submodule update --init --recursive cascade-ide
```

Собирать из корня монорепы: `dotnet build cascade-ide/CascadeIDE.sln`.

---

## Настройки до «массовых» пользователей

Автоматические миграции `settings.toml` пока не обязательны: при смене полей правь `%LocalAppData%\CascadeIDE\settings.toml` или дефолты в коде. Явные миграции — когда появятся чужие установки; см. **[ADR 0028](docs/adr/0028-user-settings-toml-localappdata-and-secrets.md)**.

---

## Лицензирование и ориентиры

- Видение open / commercial: **[docs/licensing-vision.md](docs/licensing-vision.md)**  
- Политика лицензий в коде: **[docs/license-policy.md](docs/license-policy.md)**  
- Внешний ориентир по языковому сервису (не зависимость): [RoslynPad](https://github.com/roslynpad/roslynpad); план — **[docs/LANGUAGE-SERVICES-PLAN.md](docs/LANGUAGE-SERVICES-PLAN.md)**  
