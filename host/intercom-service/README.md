# Intercom Service

Reference transport для командного Intercom ([ADR 0144](../../docs/adr/0144-intercom-team-transport-cide-sync-and-reference-service.md)): append-only events, SSE, OAuth (GitHub + generic OIDC), JWT.

Часть репозитория **cascade-ide** — `host/intercom-service/`. Сборка вместе с IDE: `CascadeIDE.slnx`.

## Стек

- ASP.NET Minimal API (.NET 10)
- **EF Core** — `IntercomDbContext`; **v1 default:** [WitDatabase](https://github.com/dmitrat/WitDatabase) (`data/intercom.witdb`, `UseWitDb`)
- **v1.1+ ops:** тот же контекст, другой провайдер EF (Postgres, SQL Server, …) — connection string из конфига ([ADR 0144 §3.1](../../docs/adr/0144-intercom-team-transport-cide-sync-and-reference-service.md))
- GitHub / OIDC → JWT для API/SSE

## Запуск (dev)

```powershell
cd host/intercom-service/src/IntercomService
dotnet run
```

Слушает `http://127.0.0.1:5080` (см. `Properties/launchSettings.json`).

### GitHub OAuth App

1. GitHub → Settings → Developer settings → OAuth App.
2. Homepage: `http://127.0.0.1:5080`
3. Callback: `http://127.0.0.1:5080/api/v1/auth/callback/github`
4. В `appsettings.Development.json` или user-secrets:

```json
"GitHub": {
  "ClientId": "<id>",
  "ClientSecret": "<secret>"
}
```

### Dev bootstrap (без GitHub)

В Development задан `DevAuth:TeamToken` — Bearer для curl/тестов:

```http
Authorization: Bearer dev-intercom-local-change-me
```

## Пилот с Tailscale

1. Запусти сервис на машине с постоянным IP в tailnet.
2. В `appsettings` укажи `Intercom:PublicBaseUrl` = `http://100.x.x.x:5080` (для OAuth callback).
3. Коллега в CIDE: `base_url` = тот же URL; Connect Intercom (GitHub).

## API (кратко)

| Метод | Путь |
|-------|------|
| GET | `/health` |
| GET | `/api/v1/auth/login?provider=github&team_id=…&redirect_uri=…` |
| GET | `/api/v1/auth/callback/github` |
| POST | `/api/v1/auth/token` |
| GET | `/api/v1/teams/{teamId}/topics` |
| POST | `/api/v1/topics/{topicId}/events` |
| GET | `/api/v1/teams/{teamId}/stream` (SSE) |

Wire: [wire/intercom-wire](../../wire/intercom-wire/README.md) · HTTP profile: [openapi.yaml](../../wire/intercom-wire/profiles/reference-http-v1/openapi.yaml) · ADR [0146](../../docs/adr/0146-intercom-wire-canonical-protocol-package.md).

## Тесты

```powershell
dotnet test CascadeIDE.slnx --filter "FullyQualifiedName~IntercomService"
```
