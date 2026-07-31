# CascadeIDE (CIDE)

Десктопная IDE на **Avalonia** и **.NET 10**: решения **`.sln` / `.slnx`**, редактор, **встроенный MCP-сервер** (`--mcp-stdio` — сборка, тесты, Git, UI и т.д.), канал **Intercom** и модели (в т.ч. локальный **[Ollama](https://ollama.com)**).

**Документация (сайт):** **[ai-guiders.github.io/cascade-ide](https://ai-guiders.github.io/cascade-ide/)** — MkDocs, русский по умолчанию, переключатель **RU / EN** в шапке. Для международной аудитории: **[Concept overview (EN)](https://ai-guiders.github.io/cascade-ide/en/concept-overview/)**.

**Лицензия кода:** [Hippocratic License 2.1](LICENSE) (Ethical Source / SPDX `Hippocratic-2.1`)

### Этическая политика (орган Cascade IDE)

Этот репозиторий — **Ethical Source**, не «чистый» OSI MIT.

- **Можно:** форкать, читать, собирать, аудитить, развивать IDE и связанный пульт.
- **Нельзя:** использовать этот код для насилия, репрессий, нарушения прав человека и иных применений, которые запрещает текст лицензии (Human Rights Principles / Human Rights Laws).
- Полный юридический текст — только в [`LICENSE`](LICENSE). Краткое резюме выше не заменяет его.
- Сторонние зависимости остаются под **своими** лицензиями — [`docs/THIRD-PARTY-NOTICES.md`](docs/THIRD-PARTY-NOTICES.md).
- Коммерческие услуги (поддержка, кастомизация, закрытый контур) — отдельно: [`docs/COMMERCIAL-NOTICE.md`](docs/COMMERCIAL-NOTICE.md). Планируемая модель: **Ethical Source + commercial**; границы — [`docs/licensing-vision.md`](docs/licensing-vision.md).

Sibling harness **cdp-mcp** на той же лицензии: https://github.com/AI-Guiders/cdp-mcp

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

Локальная сборка сайта: `pip install -r requirements-docs.txt`, `python tools/gen_adr_pages.py`, `mkdocs serve`.

---

## Возможности

| Область | Суть |
|--------|------|
| Редактор | AvalonEdit + TextMate (в т.ч. C#), правки из MCP; пакеты **[AIGuiders.AvaloniaEdit](https://github.com/KarataevDmitry/AvaloniaEdit)** |
| Решение | Дерево `.sln` / `.slnx`, открытие файлов |
| Чат | Ollama, OpenAI, Anthropic, DeepSeek, стриминг |
| MCP | [MCP protocol](https://ai-guiders.github.io/cascade-ide/MCP-PROTOCOL/) — контракт и инструменты ([исходник](docs/MCP-PROTOCOL.md)) |
| Отладка | **dotnet-debug-mcp** — [паритет человек/агент](https://ai-guiders.github.io/cascade-ide/debug-human-agent-parity-v1/) |
| Git | [Git и submodules](https://ai-guiders.github.io/cascade-ide/git-and-submodules-v1/) |
| UI | **Flight**: PFD · Forward · MFD — [раскладка](https://ai-guiders.github.io/cascade-ide/ui-ux/cascade-ide-ui-layout-v1/), [ADR 0021](https://ai-guiders.github.io/cascade-ide/adr/0021-pfd-mfd-cockpit-attention-model/) |
| Настройки | `%LocalAppData%\CascadeIDE\settings.toml`; данные рядом в WitDatabase |

<a id="до-публичного-релиза"></a>

## До публичного релиза

Пока нет массовой установки **не** наращиваем в `SettingsService` автоматические миграции при переименовании или переносе ключей в `settings.toml` — пользователь правит файл вручную или переустанавливает. Канон путей и форматов — [docs/adr/0028-user-settings-toml-localappdata-and-secrets.md](docs/adr/0028-user-settings-toml-localappdata-and-secrets.md). После публичного релиза — отдельное решение (версия файла, одноразовый мигратор, changelog).

---

## Документация

**Опубликованный сайт:** [https://ai-guiders.github.io/cascade-ide/](https://ai-guiders.github.io/cascade-ide/) · [English `/en/`](https://ai-guiders.github.io/cascade-ide/en/)

| На сайте | Зачем |
|----------|--------|
| [Навигатор ADR по статусу](https://ai-guiders.github.io/cascade-ide/site/adr-nav/) · [EN](https://ai-guiders.github.io/cascade-ide/en/site/adr-nav/) | Proposed / Accepted / Implemented |
| [Полный индекс ADR](https://ai-guiders.github.io/cascade-ide/adr/) · [EN](https://ai-guiders.github.io/cascade-ide/en/adr/) | Все решения; в репо канон RU — `docs/adr/` |
| [Архитектурная политика](https://ai-guiders.github.io/cascade-ide/architecture-policy/) · [EN](https://ai-guiders.github.io/cascade-ide/en/architecture-policy/) | Слои, таблица тем → ADR |
| [Текущая архитектура](https://ai-guiders.github.io/cascade-ide/architecture/current-architecture-v1/) · [EN](https://ai-guiders.github.io/cascade-ide/en/architecture/current-architecture-v1/) | Срез «как устроено сейчас» |
| [UI layout (Flight)](https://ai-guiders.github.io/cascade-ide/ui-ux/cascade-ide-ui-layout-v1/) · [EN](https://ai-guiders.github.io/cascade-ide/en/ui-ux/cascade-ide-ui-layout-v1/) | PFD / Forward / MFD, имена для MCP |
| [Concept overview](https://ai-guiders.github.io/cascade-ide/en/concept-overview/) | Онбординг на английском |

**В репозитории (разработка):**

| Документ | Зачем |
|----------|--------|
| [SETUP.md](SETUP.md) | Окружение: SDK, шаблоны Avalonia, Ollama |
| [Features/README.md](Features/README.md) | Оглавление `Features/` |
| [docs/architecture-migration.md](docs/architecture-migration.md) | Вынос фич из «кома» (не на сайте) |
| [docs/design/north-star-cursor-mcp-cascade-workbench-v1.md](docs/design/north-star-cursor-mcp-cascade-workbench-v1.md) | Продуктовая северная звезда |
| [samples/AcpSmokeDotnet](samples/AcpSmokeDotnet) | ACP smoke ([ADR 0016](docs/adr/0016-agent-client-protocol-external-agent.md)) |
| [docs/COMMERCIAL-NOTICE.md](docs/COMMERCIAL-NOTICE.md) | Коммерческое предложение / контакты |
| [docs/THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md) | Сторонний код |

---

## Стек

**.NET 10**, **Avalonia 12.x**, **CommunityToolkit.Mvvm**, **Roslyn**, Ollama + Microsoft.Extensions.AI для чата.

Подробный чеклист установки на машину — **[SETUP.md](SETUP.md)**.
