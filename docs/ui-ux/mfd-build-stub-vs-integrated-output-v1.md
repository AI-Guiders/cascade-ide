# MFD: Build output (v1)

**Не ADR** — рабочая заметка. Код — источник правды.

## Что в продукте сейчас (CIDE Avalonia)

- **Страница MFD:** `Views/BuildMfdPageView.axaml` — вывод сборки в MFD shell stack.
- **Логика:** `Features/Build/BuildOutputPanelViewModel` + `MainWindowBuildSessionViewModel` (оркестрация MSBuild/`dotnet build`).
- Навигация: `MfdShellPage.Build` / `show_build_output_panel`.

## Dual-HCI (Glass WPF)

- **Build output SSOT** remains CIDE Avalonia (`BuildMfdPageView` + `BuildOutputPanelViewModel`).
- **Glass WPF** MFD Build is SoftInstrument `toolchain` latch glance / stub text — no live MSBuild log host yet.
- Dig reject (2026-08-01): do not fork build orchestration into Glass TextBlock; next real peel needs WPF build-log host wired to existing build session APIs.

## Итог одной строкой

**Сборка в Cascade IDE — Avalonia Build MFD + BuildOutputPanel; Glass остаётся SoftInstrument glance до отдельного WPF host peel.**
