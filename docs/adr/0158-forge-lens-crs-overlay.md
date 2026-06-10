# ADR 0158: Forge Lens — overlay в CRS (issues/MR по текущему файлу)

**Статус:** Accepted · Implemented  
**Дата:** 2026-06-09

## Резюме

- **Forge Lens** в CIDE — read-only слой **L2** в **CRS** ([0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md)): issues и merge requests с code anchors на **текущий repo-relative файл**.
- Конфигурация **только** в `.cascade/workspace.toml` → `[workspace.forge]` (`base_url`, `repo`); Bearer — **device login** (как Intercom OAuth), не вечный PAT в env.
- Канон API, якорей и MCP-write — **вне репо:** [FORGE-ADR-0003](../../../agent-forge/design/FORGE-ADR-0003-forge-lens-cide-code-anchors.md) (agent-forge).
- Provenance в CRS: **`forge_lens`** (отдельно от `explicit_toml`, `doc_scan`, …).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0155](0155-documentation-code-correspondence-and-architectural-drift.md) | Слои L0–L4, correspondence kinds |
| [0156](0156-correspondence-mfd-surface-and-reverse-code-anchors.md) | CRS, reverse anchors, provenance |
| [0144](0144-intercom-team-transport-cide-sync-and-reference-service.md) | OAuth Connect + secrets TOML (образец) |
| [0149](0149-settings-toml-pointwise-environment-bindings.md) | `api_token_env` — только DEV override |
| [0028](0028-user-settings-toml-localappdata-and-secrets.md) | Секреты в `%LocalAppData%\CascadeIDE\` |
| FORGE-ADR-0010 | `forge auth login`, `~/.forge/credentials.json` |
| [0157](0157-cide-magic-link-protocol.md) | `cide://`; `forge://` — в FORGE-ADR-0003 |
| FORGE-ADR-0003 | Lens API, CodeAnchor, MCP-first write |

---

## Контекст

[agent-forge](../../../agent-forge/) хранит issues/MR с **CodeAnchor** (тот же контракт, что [0128](0128-intercom-attachment-anchors-and-code-references.md)). Без клиента в IDE артефакты forge остаются вне correspondence-контура CIDE.

В spike v0 появился клиент `ForgeLensCorrespondenceClient` и секция `[workspace.forge]`, но с **глобальным env-fallback** (`FORGE_BASE_URL`, `FORGE_REPO`, `FORGE_API_TOKEN`). Это скопировано с mental model **MCP-сервера** forge (один процесс = один forge), а не с модели **IDE** (несколько workspace, разные `repo`).

---

## Решение

### 1. Где живёт конфиг

| Поле | Где | Обязательность |
|------|-----|----------------|
| `base_url` | `[workspace.forge]` в `.cascade/workspace.toml` | да (для включения Lens) |
| `repo` | то же | да (имя forge-repo для этого workspace) |
| `api_token_env` | то же | нет; **только DEV** override ([0149](0149-settings-toml-pointwise-environment-bindings.md)) |

Пример:

```toml
[workspace.forge]
base_url = "http://127.0.0.1:8770"
repo = "cascade-ide"
```

### 1.1 Auth (канон, v1.1)

По образцу **Intercom** ([0144](0144-intercom-team-transport-cide-sync-and-reference-service.md) §8):

| Шаг | Действие |
|-----|----------|
| Connect | `forge_lens.connect` (IDE MCP) или `forge auth login` (CLI) |
| Approve | браузер `/view/auth/device` или `forge auth approve` (bootstrap один раз) |
| Хранение CIDE | `%LocalAppData%\CascadeIDE\forge-lens-secrets.toml` |
| Interop CLI/MCP | `~/.forge/credentials.json` (FORGE-ADR-0010) — читается, если CIDE secrets пусты |

Резолв Bearer для CRS (порядок):

1. `forge-lens-secrets.toml` для `base_url`
2. `~/.forge/credentials.json` (тот же host key)
3. `api_token_env` → env (DEV/CI)
4. без Bearer (forge за VPN без `FORGE_REQUIRE_AUTH`)

### 2. CRS integration

- Эндпоинт: `GET /api/v1/repos/{repo}/lens?file={repoRelativePath}` (контракт — FORGE-ADR-0003).
- Результаты мержатся в `WorkspaceReverseAnchorItems` с `Provenance = forge_lens`.
- `DocPath` для клика — URL issue/MR на forge (thin web / view); отдельный `forge://` handler — не в scope v1.

### 3. Отклонено: глобальный env-fallback для `base_url` / `repo`

**Не** читаем `FORGE_BASE_URL`, `FORGE_REPO` как запасной источник, если TOML пуст.

| Причина | Пояснение |
|---------|-----------|
| Workspace-scoped | У разных открытых solution разные forge-repo; глобальная env одна на машину |
| Канон CIDE | [0149](0149-settings-toml-pointwise-environment-bindings.md): env — через явный `*_env`, не magic имена |
| Копируемость | `workspace.toml` коммитится в репо (без секрета); URL/repo — свойства **проекта**, не профиля ОС |
| Пилот без TOML | Достаточно одной секции в `.cascade/workspace.toml`; не нужен shell profile |

MCP forge по-прежнему может использовать `FORGE_*` в **своём** процессе — это другой продуктовый контур.

### 4. v1 вне scope

- Slash `/forge lens`, `/forge goto` — FORGE-ADR-0003, отдельные задачи.
- Запись anchors из CIDE — MCP forge, не HTTP из CRS.
- ERS-строка «forge reachable» — опционально позже.

---

## Последствия

- Без `[workspace.forge]` CRS не показывает forge-слой (молча, как без L1 map).
- Для auth: один раз `forge_lens.connect`; bootstrap на сервере — как для первого Intercom Connect, не PAT в launch profile.
- Документация forge ↔ CIDE разделена: FORGE-ADR-0003 (протокол) + этот ADR (IDE overlay).

## Реализация (v1)

| Компонент | Путь |
|-----------|------|
| TOML models | `Features/Workspace/WorkspaceTomlModels.cs` — `RepositoryForgeToml` |
| HTTP client | `ForgeLensCorrespondenceClient.cs` |
| Credentials | `ForgeLensCredentialResolver.cs`, `ForgeLensSecretsStorage.cs`, `ForgeSharedCredentialReader.cs` |
| Device login | `ForgeLensDeviceConnectService.cs`, `forge_lens.connect` |
| CRS merge | `WorkspaceNavigationMapViewModel.Correspondence.cs` |
