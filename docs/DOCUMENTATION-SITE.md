# Сайт документации (GitHub Pages)

Публичный сайт: **https://ai-guiders.github.io/cascade-ide/**

**UX-доки** лежат в `docs/ui-ux/` (не `docs/ux/`): папка `ux` совпадала с двухбуквенным кодом языка в mkdocs-static-i18n и не попадала в сборку. На сайте: [Раскладка UI](https://ai-guiders.github.io/cascade-ide/ui-ux/cascade-ide-ui-layout-v1/) (HTML, не `.md`). Старые URL `/ux/...` редиректятся через плагин `redirects`.

### Языки (RU / EN)

| Аудитория | С чего начать |
|-----------|----------------|
| Международная (без русского) | https://ai-guiders.github.io/cascade-ide/en/concept-overview/ |
| Русскоязычная | https://ai-guiders.github.io/cascade-ide/ |

- **RU:** канон тел ADR и большинство архитектурных заметок.
- **EN:** `docs/en/concept-overview.md`, `docs/en/ui-ux/*`, навигатор ADR, **`## Summary (EN)`** в ключевых ADR (0021, 0080, 0010, 0100, 0119, 0120).
- Не использовать двухбуквенные имена папок под `docs/` кроме `docs/en/` (i18n).

## Сборка локально

```bash
pip install -r requirements-docs.txt
python tools/gen_adr_pages.py
mkdocs serve
```

Открой http://127.0.0.1:8000/ (RU) и http://127.0.0.1:8000/en/ (EN).

## Навигатор ADR

Скрипт `tools/gen_adr_pages.py` читает `**Статус:**` в шапке каждого `docs/adr/*.md` и генерирует:

- `docs/site/adr-nav/` — страницы по жизненному циклу (RU)
- `docs/en/site/adr-nav/` — то же (EN)

Канон статусов: [adr/status-lifecycle.md](adr/status-lifecycle.md).

Перед коммитом, если менял статус ADR, перезапусти генератор (CI делает это автоматически).

## CI

Workflow `.github/workflows/docs-pages.yml` — push в `develop`/`main` при изменении `docs/`, `mkdocs.yml`, генератора.

В репозитории GitHub: **Settings → Pages → Source: GitHub Actions**.

## Org hub

Лендинг организации: [ai-guiders.github.io](https://ai-guiders.github.io/) (репозиторий `AI-Guiders.github.io`).
