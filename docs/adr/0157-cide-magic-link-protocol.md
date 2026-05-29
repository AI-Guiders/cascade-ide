# ADR 0157: Magic Link `cide://` — deep link из браузера и доков в Cascade IDE

**Статус:** Accepted  
**Дата:** 2026-05-28

## Резюме

- Кастомная схема **`cide://`** (Magic Link) позволяет с публичного сайта документации, ADR на GitHub Pages или внутреннего портала **открыть Cascade IDE**, загрузить workspace и **перейти к коду или markdown**.
- v1 переиспользует **CodeAnchor / bracket** ([0128](0128-intercom-attachment-anchors-and-code-references.md), [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md)) и существующие пути **`IdeMcp.GoToPosition`** / **Markdown Preview**.
- Регистрация протокола в ОС — **opt-in** (скрипт `tools/Register-CideMagicLinkProtocol.ps1` для dev); в installer — отдельный этап.

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) | Bracket / CodeAnchor, CRS |
| [0155](0155-documentation-code-correspondence-and-architectural-drift.md) | Док ↔ код |
| [0128](0128-intercom-attachment-anchors-and-code-references.md) | `AttachmentAnchor`, bracket parse |
| [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | ADR map, preview opener |

---

## Контекст

Preview в IDE уже умеет `cascade-code-anchor:` и клик → редактор. Документация на **внешнем сайте** не может вызвать IDE без **зарегистрированного URL handler**. Аналоги: `vscode://`, `cursor://`, `jetbrains://idea/navigate/...`.

Цель: ссылка в prose ADR на сайте вида «открыть в Cascade IDE» без копирования пути.

---

## Решение

### 1. Схема и команды (v1)

| Команда (`host`) | Назначение |
|------------------|------------|
| **`reveal`** | Открыть файл в редакторе, опционально строка / bracket |
| **`open`** | Загрузить solution/workspace, затем опционально `reveal` |
| **`md`** | Открыть markdown в MFD Markdown Preview, опционально scroll |

Общие query-параметры:

| Параметр | Обязателен | Смысл |
|----------|------------|--------|
| `root` | для `open` / когда workspace ещё не загружен | Абсолютный путь к **корню workspace** (каталог с `.cascade/workspace.toml` или `.sln`) |
| `f` | для `reveal` (если нет `b`) | Repo-relative путь к файлу |
| `l`, `le` | нет | 1-based строки |
| `b` | альтернатива `f`+`l` | Bracket inner (URL-encoded): `F:…; M:…` или H1 |
| `sln` | для `open` | Относительный или абсолютный путь к `.sln` / `.slnx` |
| `doc` | для `md` | Repo-relative путь к `.md` |
| `c` | для `md` | 1-based column (зарезервировано) |

Примеры:

```text
cide://reveal?root=D:%2Frepo%2Fcascade-ide&f=Features%2FWorkspaceNavigation%2FApplication%2FDocReverseAnchorResolver.cs&l=84

cide://reveal?root=D:%2Frepo%2Fcascade-ide&b=F%3AFeatures%2FWorkspaceNavigation%2FApplication%2FDocReverseAnchorResolver.cs%3B%20M%3AResolve

cide://open?root=D:%2Frepo%2Fcascade-ide&sln=CascadeIDE.sln

cide://md?root=D:%2Frepo%2Fcascade-ide&doc=docs%2Fadr%2F0156-correspondence-mfd-surface-and-reverse-code-anchors.md&l=120
```

На HTML-сайте рядом — fallback: обычная ссылка на GitHub + подпись «требуется Cascade IDE».

### 2. Безопасность (threat model v1)

| Угроза | Митигация |
|--------|-----------|
| Произвольное чтение файлов | `root` должен пройти **workspace validation**: каталог существует и содержит `.cascade/workspace.toml` **или** discoverable `.sln` |
| Path traversal в `f` / `doc` | Нормализация; итоговый путь должен оставаться **под** `root` |
| Запуск произвольных exe | Handler только на `CascadeIDE.exe`, аргумент — один URI |
| Фишинг с поддельным `root` | Пользователь подтверждает открытие IDE; v1 без silent cross-drive |

Не поддерживается в v1: `file://`, выполнение shell, произвольные MCP-команды через URI.

**Публичный сайт (пока не делаем):** ссылка с **абсолютным** `root=D:\…` или `%USERPROFILE%\…` в HTML — утечка layout машины автора и почти всегда **битая** у читателя. Это не RCE, но фишинг (`root` на чужой клон) и путаница. Для сайта — только после **M3** (`workspace.id` → resolve на клиенте) или кнопка «скопировать bracket» без URI. До тех пор: GitHub fallback + opt-in dev script.

### 3. Single instance

| Сценарий | Поведение |
|----------|-----------|
| IDE не запущен, URI в argv | Старт IDE → после `MainWindow` Loaded — выполнить link |
| IDE запущен, второй процесс с URI | **Named pipe** `CascadeIDE.MagicLink.v1` → передать URI первому процессу → второй **exit 0** |
| IDE запущен, второй процесс без URI | Обычный второй экземпляр (не блокируем) |

Реализация: `CideMagicLinkSingleInstance` + `CideMagicLinkPipeHost`.

### 4. Код (v1)

| Компонент | Путь |
|-----------|------|
| Parse | [F:Features/MagicLink/CideMagicLinkUri.cs] |
| Execute | [F:Features/MagicLink/CideMagicLinkExecutor.cs] |
| Single instance | [F:Features/MagicLink/CideMagicLinkSingleInstance.cs] |
| VM hook | [F:ViewModels/MainWindowViewModel.MagicLink.cs] |
| Startup argv | [F:Program.cs] |
| Dev registry | [F:tools/Register-CideMagicLinkProtocol.ps1] |

Цепочка `reveal` + `b`: `BracketCodeReferenceParser.TryParse` → `TryToAttachmentAnchor` → `IdeMcp.GoToPosition` (как preview code-anchor).

### 5. Генерация ссылок для сайта (не IDE)

Рекомендация для CI/docs build (будущее):

- Шаблон из `root` = canonical clone path или placeholder `%CIDE_WORKSPACE_ROOT%` с подстановкой на машине разработчика.
- Bracket в ADR → `cide://reveal?root=…&b=…` через тот же expander, что preview (`MarkdownCodeAnchorPreviewExpander` — только схема `cide`).

---

## Альтернативы (отклонены)

| Альтернатива | Почему нет |
|--------------|------------|
| Только `vscode://` | Не открывает CRS / correspondence / Cascade MCP |
| Custom protocol без `root` | Нельзя безопасно infer workspace на чужой машине |
| HTTP localhost redirect | Firewall, второй сервер; хуже UX чем OS handler |

---

## Дорожная карта

| Этап | Работа | Критерий |
|------|--------|----------|
| **M1** | ADR 0157 + parse + `reveal`/`open`/`md` | Клик из `Register-CideMagicLinkProtocol.ps1` открывает файл |
| **M2** | Single-instance pipe | Второй клик не плодит окна |
| **M3** | Docs site generator + `workspace.id` в TOML | Стабильный `root` без абсолютного пути |
| **M4** | Installer protocol registration | Production setup |

---

## Статус реализации

| Компонент | Статус |
|-----------|--------|
| ADR 0157 | **Accepted** |
| Parse + security | **Implemented** (M1) |
| `reveal` / `open` / `md` | **Implemented** (M1) |
| Single-instance pipe | **Implemented** (M2) |
| Dev protocol script | **Implemented** (M1) |
| Docs site generator | **Deferred (M3)** — публичный сайт без `workspace.id`; не класть абсолютный `root` в HTML |
