# UiKit (IDE chrome)

Переиспользуемые **Avalonia**-компоненты оболочки IDE ([ADR 0066](../../docs/adr/0066-cockpit-ui-vs-ide-presentation-layer.md)), аналог `Views/SkiaKit/` для Skia-surfaces.

| Компонент | Назначение |
|-----------|------------|
| `Ecam*` | метрики кабины / ECAM-стиль |
| `CascadeSection` | карточка-секция (elevated surface) |
| `CascadeStatusChip` | компактный статус (loading, hint) |

Глобальные классы стилей: `App.axaml` → `cascadeSection`, `cascadeInset`, `cascadeStatusChip`, `cascadeAutocomplete`.

Токены: [ide-chrome-tokens-v1.md](../../docs/design/ide-chrome-tokens-v1.md).

**Не путать с:** `Cockpit/PrimitivesKit/` (приборы), `Views/SkiaKit/` (отрисовка чата/карт).
