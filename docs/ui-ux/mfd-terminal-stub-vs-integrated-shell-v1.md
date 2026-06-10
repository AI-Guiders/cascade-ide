# MFD: интегрированная shell-консоль (v1)

**Не ADR** — рабочая заметка по текущему терминалу в кабине. Код — источник правды.

## Что в продукте сейчас

- **Страница MFD:** `Views/TerminalMfdPageView.axaml` — `AvaloniaTerminal.TerminalControl` (ANSI, сетка, scrollback, выделение, сырой TTY-ввод).
- Макет главного окна **без** отдельной нижней полосы вкладок: терминал — **страница MFD** (`TerminalMfdPageView` в стеке `MfdShellPageStack`). См. [cascade-ide-ui-layout-v1.md](cascade-ide-ui-layout-v1.md).
- **Логика:** `Features/Terminal/TerminalPanelViewModel.cs` — `TerminalControlModel` + `IntegratedShellLaunch` (ConPTY на Windows, redirected fallback). Поток shell → `Feed(byte[])`; ввод пользователя → `UserInput` → PTY. ACP/агент дописывает вывод через `AppendOutput(string)`.
- **DAL:** `Features/Terminal/DataAcquisition/*` — адаптировано из MIT-примеров AvaloniaTerminal.

## Что ещё не заявлено

- Вкладки сессий, профили shell, интеграция с задачами сборки как в VS Code Integrated Terminal.
- Кроссплатформенный ConPTY-уровень вне Windows (сейчас redirected fallback).

## Связанные ADR / паттерны

- Транспорт потока текста — [ADR 0094](../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md) (для журнала сборки уже принят; терминал — отдельный срез).
- **Граница кокпит / shell** — [ADR 0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md).
- **Модель MFD** — [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md).

## Итог одной строкой

**Терминал в Cascade IDE — интерактивная shell-сессия с ANSI-рендером (AvaloniaTerminal + ConPTY);** расширения UX (вкладки, задачи) — отдельные срезы.
