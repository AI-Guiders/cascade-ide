# MFD: интегрированная shell-консоль (v1)

**Не ADR** — рабочая заметка по текущему терминалу в кабине. Код — источник правды.

## Что в продукте сейчас

- **Страница MFD:** `Views/TerminalMfdPageView.axaml` — `AvaloniaTerminal.TerminalControl` (ANSI, сетка, scrollback, выделение, сырой TTY-ввод).
- Макет главного окна **без** отдельной нижней полосы вкладок: терминал — **страница MFD** (`TerminalMfdPageView` в стеке `MfdShellPageStack`). См. [cascade-ide-ui-layout-v1.md](cascade-ide-ui-layout-v1.md).
- **Логика:** `Features/Terminal/TerminalPanelViewModel.cs` — `TerminalControlModel` + `IntegratedShellLaunch` (ConPTY на Windows, redirected fallback). Поток shell → `Feed(byte[])`; ввод пользователя → `UserInput` → PTY. ACP/агент дописывает вывод через `AppendOutput(string)`.
- **DAL:** `Features/Terminal/DataAcquisition/*` — адаптировано из MIT-примеров AvaloniaTerminal.

## Dual-HCI (Glass WPF)

- **Operator unHOLD 2026-08-03:** full WPF hosts (design later) — dig reject «ConPTY stays Avalonia forever» cancelled for depth wave.
- **DAL SSOT (Avalonia-free already):** `Features/Terminal/DataAcquisition/*` — `IIntegratedShellSession` · `WindowsConPtyIntegratedShellSession` · `RedirectedIntegratedShellSession` · `IntegratedShellLaunch` (byte[] DataReceived/Send/Resize). No AvaloniaTerminal refs.
- **Avalonia UI SSOT:** `TerminalMfdPageView` + `AvaloniaTerminal.TerminalControl` + `TerminalPanelViewModel` (`Feed`/`UserInput`).
- **Glass now:** redirected Process + TextBox (`GlassRedirectedShell`) — v1 presence; ANSI stripped; `TERM=dumb`.
- **Glass ConPTY peel path (dig 2026-08-03):**
  1. Extract/link DataAcquisition into Avalonia-free core (`CascadeIDE.GlassCore` or shared Terminal lib) so Avalonia + Glass share one ConPTY factory.
  2. Glass host: ConPTY session + **WPF ANSI control** (not TextBox). Dig reject 2026-08-01 still forbids ConPTY→TextBlock fork; full TTY needs VT renderer (candidate: WT-backed WPF control, or custom grid on `DataReceived`).
  3. SoftOrgan glance footnote flips to `■ Glass ConPTY` when host ships.
- Dig act (2026-08-01): Glass redirected TextBox thin peel shipped as dual-HCI instrument presence.
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
