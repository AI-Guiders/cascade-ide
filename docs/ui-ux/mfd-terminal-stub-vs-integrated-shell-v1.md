# MFD: интегрированная shell-консоль (v1)

**Не ADR** — рабочая заметка по текущему терминалу в кабине. Код — источник правды.

## Что в продукте сейчас

- **Страница MFD:** `Views/TerminalMfdPageView.axaml` — `AvaloniaTerminal.TerminalControl` (ANSI, сетка, scrollback, выделение, сырой TTY-ввод).
- Макет главного окна **без** отдельной нижней полосы вкладок: терминал — **страница MFD** (`TerminalMfdPageView` в стеке `MfdShellPageStack`). См. [cascade-ide-ui-layout-v1.md](cascade-ide-ui-layout-v1.md).
- **Логика:** `Features/Terminal/TerminalPanelViewModel.cs` — `TerminalControlModel` + `IntegratedShellLaunch` (ConPTY на Windows, redirected fallback). Поток shell → `Feed(byte[])`; ввод пользователя → `UserInput` → PTY. ACP/агент дописывает вывод через `AppendOutput(string)`.
- **DAL:** `Features/Terminal/DataAcquisition/*` — адаптировано из MIT-примеров AvaloniaTerminal.

## Dual-HCI (Glass WPF)

- **Operator unHOLD 2026-08-03:** full WPF hosts (design later) — dig reject «ConPTY stays Avalonia forever» cancelled for depth wave.
- **DAL SSOT (linked into GlassCore):** `Features/Terminal/DataAcquisition/*` compiled in `CascadeIDE.GlassCore` (Compile Include + CascadeIDE Compile Remove) — `IIntegratedShellSession` · `WindowsConPtyIntegratedShellSession` · `RedirectedIntegratedShellSession` · `IntegratedShellLaunch` (byte[] DataReceived/Send/Resize). No AvaloniaTerminal refs.
- **Avalonia UI SSOT:** `TerminalMfdPageView` + `AvaloniaTerminal.TerminalControl` + `TerminalPanelViewModel` (`Feed`/`UserInput`).
- **Glass now (2026-08-03):** shared ConPTY factory via `GlassConPtyShell` + TextBox interim (ANSI strip). Label `conpty · {shell}`. **Depth OPEN:** WPF VT control (not TextBox) — dig reject ConPTY→TextBlock as full TTY still holds.
- **Glass ConPTY peel path:**
  1. ~~Extract/link DataAcquisition → GlassCore~~ **DONE**
  2. Glass host: ConPTY session shared — **DONE** (TextBox interim); WPF VT renderer still OPEN (EasyWindowsTerminalControl / custom grid).
  3. SoftOrgan glance footnote → `■ Glass ConPTY` when VT ships (now: session shared · VT OPEN).
- Dig act (2026-08-01): Glass redirected TextBox thin peel shipped as dual-HCI instrument presence.

## Что ещё не заявлено

- Вкладки сессий, профили shell, интеграция с задачами сборки как в VS Code Integrated Terminal.
- Кроссплатформенный ConPTY-уровень вне Windows (сейчас redirected fallback).

## Связанные ADR / паттерны

- Транспорт потока текста — [ADR 0094](../adr/0094-ingestion-bus-afdx-analogy-and-threading-channels.md) (для журнала сборки уже принят; терминал — отдельный срез).
- **Граница кокпит / shell** — [ADR 0066](../adr/0066-cockpit-ui-vs-ide-presentation-layer.md).
- **Модель MFD** — [ADR 0021](../adr/0021-pfd-mfd-cockpit-attention-model.md).

## Итог одной строкой

**Терминал в Cascade IDE — интерактивная shell-сессия с ANSI-рендером (AvaloniaTerminal + ConPTY);** расширения UX (вкладки, задачи) — отдельные срезы.
