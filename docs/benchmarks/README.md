# CIDE Benchmarks

Этот каталог хранит операционные артефакты для ADR 0054:
- методика замеров;
- baseline-метрики;
- отчёты по прогонам.

## Быстрый старт

1. Выбери сценарий из ADR 0054 (`idle`, `solution_open`, `editing`, `semantic_map_file`, `semantic_map_controlFlow`, `chat_active`, `debug_session`).
2. Зафиксируй окружение (ОС, машина, build-конфиг, commit SHA).
3. Выполни минимум 3 прогона и посчитай `median` + `max`.
4. Сохрани:
   - baseline в `baselines/*.json`;
   - человекочитаемый отчёт в `reports/*.md`.

## Структура

- `baselines/` — машиночитаемые baseline-файлы.
- `reports/` — markdown-отчёты по конкретным прогонам и сравнениям.

## Минимальный JSON baseline (пример)

```json
{
  "timestamp_utc": "2026-04-17T00:00:00Z",
  "commit": "HEAD",
  "build": "Debug",
  "runtime": "net10.0/win-x64",
  "machine": "HOST-NAME",
  "scenario": "semantic_map_controlFlow",
  "metrics": {
    "working_set_mb_median": 0,
    "private_bytes_mb_median": 0,
    "cpu_avg_percent_median": 0,
    "cpu_peak_percent_max": 0,
    "map_refresh_ms_median": 0
  }
}
```

## Примечание

Сравнения между CIDE и внешними инструментами (например Cursor) сохраняй как `reference`-наблюдения с явным указанием условий, а не как жёсткий SLA.
