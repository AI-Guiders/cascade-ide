# OutWit.Common.Settings — заметки по репозиторию

Просмотр кода: [Settings](https://github.com/dmitrat/Common/tree/main/Settings), [OutWit.Common.Settings](https://github.com/dmitrat/Common/tree/main/Settings/OutWit.Common.Settings), [Json](https://github.com/dmitrat/Common/tree/main/Settings/Json/OutWit.Common.Settings.Json).

## Архитектура

- **Контейнеры** — классы, наследующие `SettingsContainer(ISettingsManager)`. Свойства перехватываются (AOP) и читают/пишут через менеджер.
- **Хранение** — по **группам** и **записям** (`SettingsEntry`: Group, Key, Value (string), ValueKind, Tag). Провайдер: `Read(group)`, `Write(group, entries)`.
- **Регистрация** — DI: `services.AddSettings(s => s.UseJson().RegisterContainer<IAppSettings, ApplicationSettings>())`. После `Build()` — `Merge()` и `Load()`.

## Json-провайдер

- `UseJson()` — файл по умолчанию `Resources/settings.json`.
- `UseJsonFile(path, scope)` — свой файл на scope (Default / User / Global).
- Формат: JSON с ключами-группами, значения — массивы `{ "key", "value", "valueKind", "tag" }`.
- User: `{UserProfile}/.config/{AppName}/settings.json`, Global: `{CommonAppData}/{AppName}/settings.json`.

## Сравнение с текущим подходом в Cascade IDE

| Сейчас | OutWit.Common.Settings |
|--------|-------------------------|
| Один TOML (settings.toml) + один JSON (ai-keys.json) | Один или несколько JSON по scope, формат «группы + entries» |
| POCO (CascadeIdeSettings, AiKeys) + ModelBase, SaveIfChanged | Контейнеры с AOP, менеджер сам грузит/сохраняет через провайдер |
| Без DI, Load/Save в сервисах | AddSettings + GetService&lt;IAppSettings&gt;() |

## TOML

В библиотеке провайдера TOML нет. Чтобы сохранить TOML, можно:

- Реализовать `ISettingsProvider` (Read/Write по группе в виде `List<SettingsEntry>`), сериализуя в TOML (например, секция = группа, ключи = Key, значения = Value). Один файл `settings.toml` или файл на группу.

## Вывод

Подключение OutWit.Common.Settings даёт типобезопасные настройки, AOP и несколько scope’ов, но меняет модель (контейнеры + группы + entries) и формат файлов, и завязан на DI. Для Cascade IDE либо вводить DI и переходить на их контейнеры и JSON, либо оставить текущий подход (ModelBase + TOML/JSON) и при желании добавить свой TOML-провайдер по образцу Json.
