# MFD: интегрированная shell-консоль (v1)

**Не ADR** — рабочая заметка по текущему терминалу в кабине. Код — источник правды.

## Что в продукте сейчас

- **Страница MFD:** `Views/TerminalMfdPageView.axaml` — `AvaloniaTerminal.TerminalControl` (ANSI, сетка, scrollback, выделение, сырой TTY-ввод).
- Макет главного окна **без** отдельной нижней полосы вкладок: терминал — **страница MFD** (`TerminalMfdPageView` в стеке `MfdShellPageStack`). См. [cascade-ide-ui-layout-v1.md](cascade-ide-ui-layout-v1.md).
- **Логика:** `Features/Terminal/TerminalPanelViewModel.cs` — `TerminalControlModel` + `IntegratedShellLaunch` (ConPTY на Windows, redirected fallback). Поток shell → `Feed(byte[])`; ввод пользователя → `UserInput` → PTY. ACP/агент дописывает вывод через `AppendOutput(string)`.
- **DAL:** `Features/Terminal/DataAcquisition/*` — адаптировано из MIT-примеров AvaloniaTerminal.

## Dual-HCI (Glass WPF)

- **Operator unHOLD 2026-08-03:** full WPF hosts (design later) — dig reject «ConPTY stays Avalonia forever» cancelled for depth wave.
- **Glass now (2026-08-04):** `EasyWindowsTerminalControl` (WT WPF VT) — Avalonia EOL, cabin takes ready WPF terminal. Launch cmdline from GlassCore `IntegratedShellLaunch`. SoftInstrument: `■ Glass VT`.
- **Glass ConPTY peel path:**
  1. ~~Extract/link DataAcquisition → GlassCore~~ **DONE**
  2. ~~Glass WPF VT host~~ **DONE** (`EasyWindowsTerminalControl` · not TextBox)
  3. SoftInstrument glance → `■ Glass VT` when lived dogfood.

## Что ещё не заявлено

- Вкладки сессий, профили shell, интеграция с задачами сборки как в VS Code Integrated Terminal.
- Кроссплатформенный ConPTY-уровень вне Windows (сейчас redirected fallback).

## Связанные ADR / паттерны

- Транспорт потока текста — [ADR 0094](../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md) (для журнала сборки уже принят; терминал — отдельный срез).
- **Граница кокпит / shell** — [ADR 0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md).
- **Модель MFD** — [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md).

## Итог одной строкой

**Cabin (Glass):** EasyWindowsTerminalControl VT · Avalonia EOL. **Agent-IDE Avalonia:** AvaloniaTerminal + ConPTY still in tree until agents migrate; not cabin SSOT.
