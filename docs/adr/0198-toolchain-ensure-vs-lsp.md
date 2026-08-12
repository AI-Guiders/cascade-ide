# ADR 0198: Toolchain ensure vs LSP

**Статус:** Accepted  
**Дата:** 2026-07-27  
**Tags:** #cdp #toolchain #lsp #dal #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0040](0040-lsp-launch-line-settings-toml-presets-and-environment.md) | LSP presets |
| [0102](0102-data-acquisition-layer-boundary-and-contract.md) | DAL — добыча внешнего |
| [0190](0190-agent-ide-settings-organ.md) | `lsp_ensure` (intelligence) |
| [0197](0197-cdp-mcp-cockpit-wire-parity-vs-cide.md) | Шов DAL; gap board |
| [0196](0196-architecture-staging-board-arch-desk.md) | Soft-instrument pattern |

## Резюме

- **Toolchain** ≠ **LSP**: runtime/compiler/SDK vs language server.
- Один ensure-контур для **любого** id (`python`, `gcc`, `javac`, `go`, custom): `probe|ensure|install|add|which`.
- Soft organ: `go=toolchain` / Meta `cdp_toolchain` (не ListTools thrash).
- Hang: **DAL-adjacent** ([0197](0197-cdp-mcp-cockpit-wire-parity-vs-cide.md)); не alias на `lsp_ensure`.

## Контекст

`lsp_ensure id=python` ставит basedpyright, не CPython. Оператору нужен gesture «сегодня python/gcc/go» → bins на PATH. Расширение = новая recipe-запись, не новый organ.

## Решение

### Оси

| Axis | Organ | Пример |
|------|-------|--------|
| Intelligence | `lsp_*` / Options languages | pyright, gopls |
| Toolchain | `toolchain_*` | python, gcc, javac, go |

### Ops v0

| op | смысл |
|----|--------|
| `scene` | каталог recipes + probe status |
| `probe id=` | bins на PATH |
| `ensure id=` | already_ok \| install via recipe \| no_recipe → browser/shell next[] |
| `install id= via=` | явный менеджер |
| `add` | user recipe (id, bins[], vias[], search_q, pairs_lsp?) |
| `which id=` | resolved paths |

### Recipe

```text
ToolchainRecipe {
  id, bins[], vias[{via, argv[]}], search_q,
  pairs_lsp?, roles[]  // runtime|compiler|sdk
}
```

Built-ins v0: **python**, **gcc**, **javac**, **go** (равные граждане). Custom через `add` → LocalAppData user presets.

### Инварианты

- Один код-путь; нет `if (python)`.
- После ensure опционально `next[]` → `lsp_ensure` если `pairs_lsp`.
- Install через IDE shell (как LSP); PATH refresh может требовать remount.

## Не делать

- Подменять `lsp_ensure`.
- UI магазина пакетов.
- Полный multi-language `cdp_build` router в v0 (только hint → ensure).

## Последствия

- cdp-mcp: `IdeToolchainChannel` + GoMap + soft Meta.
- Onboard/options могут давать next[] `toolchain_ensure`.
