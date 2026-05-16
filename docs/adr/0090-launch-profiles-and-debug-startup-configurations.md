# ADR 0090: Профили запуска и несколько стартовых конфигураций отладки (как launch profiles в VS)

**Статус:** Accepted · Implemented  
**Дата:** 2026-04-23  
## Связанные ADR

| ADR | Роль |
|-----|------|
| [0002](0002-debug-human-agent-parity.md) | единый слой отладки для человека и агента |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | Пользовательские настройки — `settings.toml`, каталог `%LocalAppData%\CascadeIDE\`, секреты отдельно |
| [0029](0029-configuration-toml-canonical-ui-facade.md) | Конфигурация — **TOML-first** (канон на диске); **целостный** UI настроек — **deferred**; точечный UI — **фасад канона**, не вторая правда |
| [0093](0093-mfd-embedded-browser-for-launch-url.md) | Встроенный просмотр URL запуска на MFD (расширение к профилям и launchBrowser) |

### Вне ADR

| Документ | Роль |
|----------|------|
| [MCP-PROTOCOL.md](../MCP-PROTOCOL.md) | `debug_launch` и родственные тулы |
| [MsBuildDebugTargetResolver.cs](../../Services/MsBuildDebugTargetResolver.cs) | MsBuildDebugTargetResolver.cs |

## Контекст

Сейчас на **одно решение** приходится **не больше одного** явного стартового проекта: путь к `.csproj` хранится в `.cascade-ide/startup-project.json` как одно поле `StartupProjectRelativePath`. F5 и интерактивный `debug_launch` резолвят **одну** цель; конфигурация MSBuild для отладки фактически зашита как **Debug** в резолвере.

В Visual Studio и в SDK-проектах привычна другая модель: **несколько именованных профилей** (launch profiles) — разные проекты, конфигурации (Debug/Release/…), аргументы командной строки, рабочий каталог, переменные окружения. Разработчик переключает «текущий профиль» и жмёт F5, не переустанавливая «стартовый проект» вручную каждый раз.

Без этого Cascade IDE остаётся ближе к «один старт на solution», что слабо масштабируется на монорепы, несколько исполняемых в одном `.sln` и сценарии «запусти API / консоль / тест-хост» без отдельного файлового диалога.

**Веб (ASP.NET Core)** — не «потом»: типичные портальные/SPA-host решения (в т.ч. продуктовый контур **EDW.Portal**, scope `portal` в оперативной памяти) по сути **зависят** от `Properties/launchSettings.json`: Kestrel **URL** (`http://localhost:…` / `https://…`, несколько endpoints), `ASPNETCORE_ENVIRONMENT`, иногда `launchBrowser`, разные профили (`http` / `https` / IIS). Без маппинга этих полей в DAP-launch (переменные окружения процесса + при необходимости аргументы хоста) F5 в CIDE **не** совпадёт с привычным `dotnet run` / VS. Консольные exe — подмножество; **веб — обязательный** горизонт v1 согласно этому ADR (детализация полей — ниже).

<a id="adr0090-decision"></a>

## Решение (направление)

Ввести **каталог профилей запуска** (launch profiles) в зоне workspace, с **текущим выбранным профилем**, и прогонять отладку (DAP launch) **через активный профиль**, а не через единственный `StartupProjectRelativePath`.

<a id="adr0090-profile-model-v1"></a>

### Минимальная модель профиля (v1)

Для каждого профиля, как минимум:

- **Идентификатор** — стабильное имя в пределах решения (строка; отображаемое имя может совпадать или локализоваться отдельно).
- **Проект** — путь к `.csproj` / `.fsproj` **относительно корня «решения»** (тот же базовый каталог, что и для `BreakpointsFileService.GetWorkspaceRoot(solutionPath)`), в духе текущего store.
- **Конфигурация MSBuild** — например `Debug` / `Release` (строка; по умолчанию `Debug` для отладки).
- **Аргументы программы** — опционально, список строк или одна строка с правилами кавычек (уточнить при реализации).
- **Рабочий каталог** — опционально; если пусто — наследовать правила из [IdeDapDebugSession](../../Services/IdeDapDebugSession.cs) (уже есть логика вокруг `ResolveLaunchWorkingDirectory`).

- **Переменные окружения** процесса — для веба как минимум передавать/мерджить с тем, что приходит из профиля (часто `ASPNETCORE_ENVIRONMENT=Development` и кастомные ключи). Реализация: env в DAP `launch` (расширение [IdeDapDebugSession](../../Services/IdeDapDebugSession.cs), если сейчас нет), без «тихого» отбрасывания.

<a id="adr0090-aspnet-v1"></a>

### ASP.NET Core / веб (v1, не откладывать)

Профиль должен уметь выражать то, что **уже** в `launchSettings.json` для `commandName: Project` (Kestrel):

- **`applicationUrl`** — одна или несколько привязок (строка с `;` как в шаблоне SDK, либо массив URL в TOML); при запуске подставляется в окружение так же, как делает `dotnet` (типично `ASPNETCORE_URLS` или согласованный с хостом способ — уточнить при реализации по `dotnet` / hosts).
- **`launchBrowser`** — опционально; при `true` открытие URL после старта (если в CIDE появляется обёртка; иначе зафиксировать «deferred / v2» явно, но не молчать).
- **Раздельные профили** вроде `http` / `https` — несколько именованных записей в TOML, как в `launchSettings.profiles`, с импортом 1:1 по именам, где возможно.

**IIS Express** (`commandName: IISExpress`) и полный паритет с VS для IIS — **может** оказаться отдельной фазой (другой процесс, другой путь к рабочему каталогу); в ADR зафиксировать **минимум**: Kestrel + `Project` — baseline для веб v1, IIS — либо «best effort», либо отдельный тикет после baseline.

**Импорт:** при чтении `Properties/launchSettings.json` веб-проекта схема TOML в `.cascade-ide` должна **сохранять семантику** URL и env, чтобы сценарии вроде портала не ломались относительно `dotnet run --launch-profile …`.

<a id="adr0090-storage"></a>

### Хранение

- **Каноничный формат: TOML**, не JSON — например `.cascade-ide/launch-profiles.toml` (плюс ключ/секция версии схемы, напр. `version = 1`). Такая же философия, что и пользовательский/канонический конфиг в IDE ([0028](0028-user-settings-toml-localappdata-and-secrets.md), [0029](0029-configuration-toml-canonical-ui-facade.md)): **читаемость, комментарии, один стиль** с `settings` / `workspace` рядом с репозиторием.
- **Один файл** на открытое решение (путь к `.sln` / standalone `.csproj` — как сейчас для брейкпоинтов и startup): в нём и **список профилей**, и **активный** профиль (`active_profile` / аналог — уточнить при реализации), без второго файла, чтобы не рассинхронизировать.
- **JSON** (`Properties/launchSettings.json`) остаётся **внешним** де-факто стандартом SDK/VS — не дублировать его как основной канон в `.cascade-ide`, а **импортировать/экспортировать** в TOML-модель (маппинг полей, см. «Согласование с .NET»).

**Миграция:** если существует только `startup-project.json`, при первом чтении построить **один** профиль по умолчанию (имя вроде `Default` / из имени проекта), выставить его активным, записать `launch-profiles.toml`, старый JSON оставить как совместимость до явного удаления (strangler).

<a id="adr0090-ui"></a>

### UI

- Видимый **селектор текущего профиля** (toolbar или полоса у обозревателя / баннер отладки) — **keyboard-first** согласованно с [0013](0013-command-surface-and-discoverability.md).
- Команды: «управление профилями» (добавить/удалить/дублировать) — MFD или модалка по объёму ([0074](0074-settings-ui-mfd-compact-layout-overflow.md) как политика нехватки места).
- F5 / «запустить отладку» используют **активный профиль**, без диалога выбора `.dll`, если резолв успешен (согласуется с паритетом [0002](0002-debug-human-agent-parity.md)).

<a id="adr0090-mcp-parity"></a>

### Паритет агента (MCP / IdeCommands) — контракт v1

Контракт запуска отладки фиксируется на уровне `IdeCommands.DebugLaunch` и `MCP-PROTOCOL`:

- Команда: `debug_launch`.
- Режим A (явная цель): `workspace_path` + `target_path` (как сейчас, backward compatible).
- Режим B (профиль): `profile_name` (опционально) + контекст открытого workspace/solution.
  - если `profile_name` не задан, используется `active_profile`;
  - если профиль не найден — явная ошибка контракта (не молчаливый fallback на «случайный» startup).
- Доп. параметры остаются: `netcoredbg_path`, `program_args`.
- При одновременной передаче `target_path` и `profile_name` приоритет у `target_path` (явный путь агента сильнее профиля).

Это позволяет человеку и агенту использовать один смысл F5/Launch: «запуск по активному профилю», а для точечных сценариев сохраняется прямой запуск по `target_path`.

<a id="adr0090-dotnet-alignment"></a>

### Согласование с .NET

- **Опциональный импорт** из `Properties/launchSettings.json` (после v1) — уменьшить трение для проектов, уже настроенных под `dotnet` / VS. Канон по-прежнему **TOML в `.cascade-ide`**; с **стандартным** `launchSettings` обеспечивается **семантическая совместимость** (маппинг полей), а не побитовая идентичность одного файла.
- По желанию **экспорт** профиля обратно в форму, близкую к `launchSettings`, для совместимости с другими инструментами — без обязательности.

<a id="adr0090-build-resolve"></a>

### Резолв сборки

- [MsBuildDebugTargetResolver](../../Services/MsBuildDebugTargetResolver.cs): передавать **конфигурацию** из профиля (`-p:Configuration=...`), а не только константу `Debug`, когда источник истины — активный профиль.

<a id="adr0090-consequences"></a>

## Последствия

- Потребуется рефакторинг `MainWindowViewModel` / `StartupProjectStore` в сторону **модели «набор + активный»**; старые API — strangler.
- Тесты: парсинг TOML, миграция с одного `startup-project.json`, импорт **веб-**`launchSettings` (см. пример в репо: [OpenVoiceTts.Service launchSettings.json](../../../voice-tts/OpenVoiceTts.Service/Properties/launchSettings.json) — `applicationUrl`, `environmentVariables`), сценарий F5: консоль + **Kestrel** с валидным URL.
- Документация для пользователя (User Guide) — продуктовый слой, не обязательный объём самого ADR (см. [README](README.md) про User Guide).

<a id="adr0090-rejected"></a>

## Отклонённые / отложенные альтернативы

- **Только** читать `launchSettings.json` и не иметь собственного файла — отклонено как хуже для MVP паритета IDE (монорепы, пути относительно solution, и агенту нужен стабильный контракт в `.cascade-ide` рядом с брейкпоинтами).
- **Несколько параллельных отладок** в одной IDE — вне scope этого ADR (остаётся одна DAP-сессия, как сейчас, если иное не зафиксировано отдельно).

<a id="adr0090-implementation-status"></a>

## Статус внедрения

- Канон хранения: `.cascade-ide/launch-profiles.toml`, миграция из `startup-project.json`, `MsBuildDebugTargetResolver` с конфигурацией из профиля, DAP `launch` с `env` и опциональным cwd, `debug_launch` с `profile_name` и явными ошибками контракта; тесты `LaunchProfilesStoreTests`, документация `IdeCommands` / `MCP-PROTOCOL.md`.

<a id="adr0090-implementation-checklist"></a>

## Implementation checklist (из решения в код)

1. Ввести `.cascade-ide/launch-profiles.toml` (список профилей + `active_profile`) и миграцию из `startup-project.json`.
2. Обновить резолв отладки: `MsBuildDebugTargetResolver` получает `Configuration` из профиля (не только `Debug`).
3. Обновить `debug_launch` pipeline:
   - поддержка `profile_name`,
   - однозначные ошибки (`profile_not_found`, `active_profile_missing`, `profile_target_unresolved`),
   - совместимость с текущим `workspace_path + target_path`.
4. Синхронизировать документацию:
   - XML-doc `IdeCommands.DebugLaunch`,
   - `docs/MCP-PROTOCOL.md` (сгенерированный блок),
   - при необходимости ADR-индекс/ссылки.
5. Test plan (минимум):
   - консольный проект: профили `Debug`/`Release` с разными аргументами;
   - ASP.NET Core (Kestrel): импорт `applicationUrl`/env и запуск по профилю;
   - негатив: отсутствующий профиль и неразрешимая цель дают явную ошибку.
