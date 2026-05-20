# Intercom — справка

Локальное сообщение (`audience: self`): видно только тебе на этом клиенте, не уходит агенту и во внешний канал.

## Навигация и темы

- `/topic cards` · `/topic open <id|заголовок>` — картотека и detail темы
- `/topic create <название>` · `/topic list` · `/topic tree` — списки (текст: `… list text`)
- `/spine list` · `/spine tree` · `/spine focus=…` — product spine
- `/card <заголовок>` — новая тема (алиас create)

## Сообщения агенту

- Обычный текст в composer → отправка агенту (Enter по настройке send).
- `@` — люди/упоминания; `[ … ]` — артефакты кода и вложения (не markdown-ссылки).

## Вложения в тексте `[ … ]`

- Оси: `F:` путь · `M:` член · `L:` строки · `S:` scope (напр. `S:for:2` — 2-й `for` в теле `M:…`)
- Примеры: `[M:Run]` · `[Foo.cs M:Bar]` · `[M:Run S:for:1]` · `[F:src/X.cs; M:Y; L:10-20]`
- Autocomplete: набери `[` — подсказки по осям; Tab/Enter — вставить; Esc — закрыть.
- `/attach selection` · `/attach scope` · `/attach file <path> [start] [end]`
- Маркеры `⟦a:…⟧` в черновике — chip; клик в ленте → reveal в редакторе.

## Слэш-команды IDE и Intercom

- `/` — autocomplete (Tab — выбор, Enter — выполнить).
- `/help <namespace>` — только список команд (напр. `/help topic`, `/help attach`).
- `/export` — Markdown сессии; `/inspect spine` — JSON spine.

---

Редактируй этот файл: `Intercom/intercom-help.ru.md` (рядом с exe или встроенный ресурс сборки).
