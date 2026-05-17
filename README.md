# CascadeIDE (CIDE)

Десктопная IDE на **Avalonia** и **.NET 10**: решения **`.sln` / `.slnx`**, редактор, **встроенный MCP-сервер** (`--mcp-stdio` — сборка, тесты, Git, UI и т.д.), панели **чат / модели** (в т.ч. локальный **[Ollama](https://ollama.com)**).

**Лицензия кода:** [MIT](LICENSE)

Использование исходников и сборок **внутри условий MIT** (сохранение уведомлений, лицензии и т.д.) — как обычно для open-source. Если тебе нужно **коммерческое** применение (продукт на основе IDE, встраивание в закрытый контур, поддержка, кастомизация под бизнес) — **свяжись с нами**: контакты и рамки в **[docs/COMMERCIAL-NOTICE.md](docs/COMMERCIAL-NOTICE.md)**. Планируемая модель: **MIT + commercial** (открытый код + отдельная коммерческая линейка/услуги); подробнее о границах — **[docs/licensing-vision.md](docs/licensing-vision.md)**.

---

## Быстрый старт

Нужны [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) и ОС с поддержкой Avalonia (Windows / Linux / macOS). Для локального чата — запущенный Ollama (`http://localhost:11434`).

```bash
git clone https://github.com/AI-Guiders/cascade-ide.git
cd cascade-ide
dotnet restore
dotnet build CascadeIDE.sln -c Release
dotnet run --project CascadeIDE.csproj
```

При старте приложение проверяет Ollama и подскажет, если API недоступен.

Итерация без тяжёлой генерации в сборке:

```bash
dotnet build CascadeIDE.sln -c Debug -p:FastBuild=true
dotnet run --project CascadeIDE.csproj -p:FastBuild=true
```

**Документация (сайт):** [ai-guiders.github.io/cascade-ide](https://ai-guiders.github.io/cascade-ide/) — MkDocs, RU/EN. **English onboarding:** [concept overview](https://ai-guiders.github.io/cascade-ide/en/concept-overview/). [Навигатор ADR](https://ai-guiders.github.io/cascade-ide/site/adr-nav/). Локально: `pip install -r requirements-docs.txt`, `python tools/gen_adr_pages.py`, `mkdocs serve`.

---

## Возможности

| Область | Суть |
|--------|------|
| Редактор | AvalonEdit + TextMate (в т.ч. C#), правки из MCP; пакеты **[AIGuiders.AvaloniaEdit](https://github.com/KarataevDmitry/AvaloniaEdit)** |
| Решение | Дерево `.sln` / `.slnx`, открытие файлов |
| Чат | Ollama, OpenAI, Anthropic, DeepSeek, стриминг |
| MCP | [docs/MCP-PROTOCOL.md](docs/MCP-PROTOCOL.md) — контракт и список инструментов |
| Отладка | Связка с **dotnet-debug-mcp**: [docs/debug-human-agent-parity-v1.md](docs/debug-human-agent-parity-v1.md) |
| Git | [docs/git-and-submodules-v1.md](docs/git-and-submodules-v1.md) |
| UI | Режимы Focus / Balanced / Power — хоткеи в приложении |
| Настройки | `%LocalAppData%\CascadeIDE\settings.toml`; данные рядом в WitDatabase |

<a id="до-публичного-релиза"></a>

## До публичного релиза

Пока нет массовой установки **не** наращиваем в `SettingsService` автоматические миграции при переименовании или переносе ключей в `settings.toml` — пользователь правит файл вручную или переустанавливает. Канон путей и форматов — [docs/adr/0028-user-settings-toml-localappdata-and-secrets.md](docs/adr/0028-user-settings-toml-localappdata-and-secrets.md). После публичного релиза — отдельное решение (версия файла, одноразовый мигратор, changelog).

---

## Документация

| Документ | Зачем |
|----------|--------|
| [SETUP.md](SETUP.md) | Окружение: SDK, шаблоны Avalonia, Ollama |
| [docs/architecture-policy.md](docs/architecture-policy.md) | Слои и правила изменений в коде |
| [docs/architecture-migration.md](docs/architecture-migration.md) | Вынос фич из «кома» |
| [Features/README.md](Features/README.md) | Оглавление `Features/` |
| [docs/ui-ux/README.md](docs/ui-ux/README.md) | Раскладка окна, зоны для автоматизации |
| [docs/design/north-star-cursor-mcp-cascade-workbench-v1.md](docs/design/north-star-cursor-mcp-cascade-workbench-v1.md) | Продуктовая северная звезда |
| [docs/adr/0016-agent-client-protocol-external-agent.md](docs/adr/0016-agent-client-protocol-external-agent.md) | ACP: вендор `externals/acp-csharp`, sample [samples/AcpSmokeDotnet](samples/AcpSmokeDotnet) |
| [docs/adr/0028-user-settings-toml-localappdata-and-secrets.md](docs/adr/0028-user-settings-toml-localappdata-and-secrets.md) | Настройки, секреты, миграции `settings.toml` |
| [docs/backlog-ideas-from-doc-pipeline-v1.md](docs/backlog-ideas-from-doc-pipeline-v1.md) | Отложенные идеи |
| [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md) | Сторонний код |
| [docs/COMMERCIAL-NOTICE.md](docs/COMMERCIAL-NOTICE.md) | Коммерческое предложение / контакты |
| [docs/cursor-rules-examples.md](docs/cursor-rules-examples.md) | Примеры `.cursor/rules` |
| [docs/licensing-vision.md](docs/licensing-vision.md) | Open vs commercial |
| [docs/license-policy.md](docs/license-policy.md) | Политика лицензий в репозитории |
| [docs/LANGUAGE-SERVICES-PLAN.md](docs/LANGUAGE-SERVICES-PLAN.md) | План языкового сервиса (ориентир [RoslynPad](https://github.com/roslynpad/roslynpad)) |

---

## Стек

**.NET 10**, **Avalonia 12.x**, **CommunityToolkit.Mvvm**, **Roslyn**, Ollama + Microsoft.Extensions.AI для чата.

Подробный чеклист установки на машину — **[SETUP.md](SETUP.md)**.
