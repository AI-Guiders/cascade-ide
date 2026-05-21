# ADR 0139: Hot-reload типографики `[fonts]` — только режим Ground

**Статус:** Proposed · **Ground-only** (обсуждение до реализации)  
**Дата:** 2026-05-21

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | `%LocalAppData%\CascadeIDE\settings.toml` |
| [0123](0123-intercom-full-skia-surface-evolution.md) | Skia Intercom + MFD typography |
| [0127](0127-intercom-spine-and-topic-tabs-chrome-navigation.md) | Chrome navigation — не смешивать с reload settings |
| [0010](0010-ui-modes-toml-configuration.md) | UiModes / слои продукта |

## Резюме

- **Сделать:** при сохранении `settings.toml` (или явном «Применить») перечитывать `[fonts.intercom]` / `[fonts.editor]` и проталкивать в VM → Skia (`IntercomPanelFontsChanged`) + Avalonia MFD (`ChatPanelTypographyApplier`), с инвалидацией `IntercomFontDefaults`.
- **Не делать в Take Off:** фоновый watcher файла, debounce, частичный merge без перезапуска IDE, hot-reload всего `CascadeIdeSettings`, hot-reload `display.screens` / topology.
- **Почему Ground:** смена pt/family без перезапуска — **настройка и калибровка** (как темы/UiModes на земле), а не полётный контур, где гонки reload vs layout/presentation недопустимы.

## Контекст

После embedded `Settings/defaults-settings.toml` ([0028](0028-user-settings-toml-localappdata-and-secrets.md)) типографика стабильна при старте, но правка `%LocalAppData%\CascadeIDE\settings.toml` требует **полного перезапуска IDE**. Оператору неудобно подбирать `prose_pt`, `panel_*`, editor `size_pt`.

## Решение (направление)

1. **Триггер:** `SettingsService.Save` успешно записал файл **и** секция `[fonts]` изменилась (или первый load после edit в UI настроек, если появится).
2. **Путь данных:** `SettingsDefaultsLoader.DeserializeEffective(diskToml)` → сравнить `Fonts` → если diff → event `FontsSettingsChanged`.
3. **Подписчики:** `MainWindowViewModel` / `ChatPanelViewModel.SetIntercomFontsSettings`, `IntercomFontDefaults.Reset()`, `InvalidateVisual` на `IntercomSkiaSurface`.
4. **Редактор:** `DockDocumentView.ApplyEditorFontFromSettings` для открытых документов (опционально v1 — только активный editor).
5. **Не в scope v1:** FileSystemWatcher; reload при внешнем редактировании toml в Notepad.

## Отклонено (Take Off)

| Идея | Почему |
|------|--------|
| Watcher на `settings.toml` | Непредсказуемый reload mid-flight, конфликт с Save merge presentation |
| Hot-reload всего settings | Слишком широко; display/ai требуют отдельных ADR |
| Reload без invalidation кэша | Риск stale `IntercomFontDefaults` |

## Критерий готовности (когда Accepted)

1. Изменить `prose_pt` в user `settings.toml`, нажать Save (или переключить фокус с сохранением) — **без перезапуска** обновляются Skia-лента и MFD Chat.
2. Forward `prose_pt_forward` и MFD `prose_pt` расходятся корректно после reload.
3. Регрессия: embedded defaults при отсутствии user-файла не ломаются.

## История

| Дата | Изменение |
|------|-----------|
| 2026-05-21 | Proposed: Ground-only; отложено до завершения 0127 A/B. |
