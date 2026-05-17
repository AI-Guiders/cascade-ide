# Сайт документации (GitHub Pages)

Публичный сайт: **https://ai-guiders.github.io/cascade-ide/**

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
