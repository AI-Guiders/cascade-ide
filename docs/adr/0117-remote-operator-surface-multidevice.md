# ADR 0117: Remote operator surface — мультидевайсность оператора (пульт, не mobile IDE)

**Статус:** Proposed  
**Дата:** 2026-05-16  
**Обновлено:** 2026-05-16 — клиент remote surface: **PWA** (каноничный выбор).

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0017](0017-multi-window-workspace-and-agent-surfaces.md) | Мультиоконность на одной станции (**не** remote) |
| [0108](0108-web-ai-portal-host-object-tools-bridge.md) | Веб в MFD → `IdeCommands` (ортогонально) |
| [0035](0035-mfd-embedded-webview-external-llm-and-mcp-boundary.md) | Граница доверия веб ↔ MCP |
| [0016](0016-agent-client-protocol-external-agent.md) | Внешний агент по ACP |
| [0031](0031-agent-chat-clarification-batches-and-threading.md) | Уточнения Intercom vs PFD |
| [0030](0030-command-ids-hotkeys-and-ui-registry-layers.md) | `command_id`, подтверждения |
| [0008](0008-mcp-contracts-and-testable-infrastructure.md) | `IdeCommands`, MCP |
| [0043](0043-mcp-transport-recovery-human-agent-parity.md) | Восстановление транспорта |
| [0099](0099-ide-databus-typed-events-and-projections.md) | События IDE → проекции |
| [0045](0045-agent-chat-persistence-event-log-and-projections.md) | История чата / события |

**Вне репо:** vision **agent-forge** (худший сценарий, API + браузер) — личный канон `agent-notes`.

---

## 1. Контекст

**Силы:**

- Оператор (человек) работает не только за **одним** рабочим столом: второй ПК, планшет, телефон — для **наблюдения**, **ответа агенту**, **подтверждений**, смены режима WORK/HUMAN, без переноса полного IDE.
- **[ADR 0017](0017-multi-window-workspace-and-agent-surfaces.md)** решает **мультимониторность на одной станции** (`MainWindow`, `MfdHostWindow`, пресеты `presentation`). Это **не** сессия «ушёл из кабинета — продолжил с телефона».
- **[ADR 0108](0108-web-ai-portal-host-object-tools-bridge.md)** даёт **встроенный** веб в MFD с мостом к IDE для **внешней** веб-модели. Это не **пульт оператора** с другого устройства и не замена нативного Intercom.
- Линия **Friction / environment-first:** удобство меряется по **худшему пути** (слабая сеть, только браузер, первый раз). Если там нельзя **увидеть статус** и **принять решение**, а тяжёлое закрытие задачи возможно только за десктопом — это приемлемо **только** если десктоп явно в контуре; иначе оператор на втором устройстве — «второй сорт».

**Проблема:** без явной архитектуры легко скатиться в (a) **mobile IDE** — нереалистичный паритет Roslyn/отладки; (b) **голый чат** без привязки к сессии IDE; (c) **небезопасный** открытый порт без pairing.

---

## 2. Решение (намерение)

### 2.1. Три контура (разделение ответственности)

| Контур | Где | Назначение |
|--------|-----|------------|
| **Тяжёлый (cockpit)** | CascadeIDE на рабочей станции | редактор, Roslyn, build, отладка, полный HUD, MCP в процессе |
| **Лёгкий (remote operator surface)** | **PWA** на другом устройстве (телефон, планшет, второй ПК) | статус сессии, Intercom (ограниченно), approve/reject, pause/stop, уведомления «агент ждёт» |
| **Машинный** | git, MCP remote, CI, Issues | закрытие задачи без UI; агент и автоматизация |

**Принцип:** remote surface **не** дублирует cockpit. Он **наблюдает и решает**; исполнение тяжёлого — на станции с IDE или через уже существующие **MCP / `IdeCommands`** (с политикой на gateway).

### 2.2. Remote operator surface (ROS) — scope v1

**Входит (целевая фаза 1–2):**

- Подписка на **снимок состояния** сессии: workspace id, активная тема/тред Intercom (кратко), флаг «ожидает человека», последние N сообщений агента, IDE health / failed step (без утечки полного workspace tree).
- Действия оператора с **явным подтверждением** на десктопе при риске: ответ в чат (follow-up), approve/reject для зон «только человек» ([0030](0030-command-ids-hotkeys-and-ui-registry-layers.md), pre-flight — [0042](0042-pre-flight-planned-changes-and-review-before-apply.md) когда появится).
- **Push / pull уведомления:** «агент заблокирован на вопросе» (интеграция с внешним каналом — Telegram, email — **вне** ядра ADR, через адаптер).
- **Pairing** удалённого клиента с инстансом IDE (одноразовый код, TTL, отзыв).

**Не входит в v1:**

- Полноценный редактор кода в браузере.
- Произвольный вызов всех `IdeCommands` с телефона без allowlist.
- Публичный облачный relay без E2E / без привязки к паре устройств.
- **Нативные** companion-приложения (iOS/Android/desktop) — отложены; при необходимости позже как оболочка над тем же API gateway.

### 2.2.1. Клиент: PWA (канон)

**Решение:** remote operator surface реализуется как **Progressive Web App**, а не как отдельные нативные приложения и не как «просто вкладка без install».

**Почему PWA:**

| Критерий | PWA |
|----------|-----|
| Один артефакт | телефон, планшет, второй ПК — один UI (HTML/CSS/TS), без store-релизов |
| Friction / худший путь | «открыл в браузере → Add to Home Screen» без установки CIDE |
| Транспорт | тот же **Operator Gateway** (SSE/WebSocket + REST); PWA — только клиент |
| Уведомления (фаза 3) | **Web Push** + service worker при явном согласии; fallback — pull/SSE |
| Офлайн | service worker: кэш оболочки UI + последний snapshot; **команды** при offline — очередь или отказ (см. §7) |
| Безопасность | origin пульта = origin gateway (loopback или VPN); **не** смешать с [0108](0108-web-ai-portal-host-object-tools-bridge.md) |

**Поставка:** статика PWA раздаётся **с того же хоста**, что Operator Gateway (например `Features/OperatorGateway/www/` или embedded `wwwroot`), чтобы не плодить CORS и второй порт без нужды. После pairing пользователь сохраняет ярлык «Cascade Operator».

**Минимальный UX v1:** одна страница «сессия» (статус + лента + поле ответа); mobile-first layout; без сложной навигации кокпита.

### 2.3. Транспорт и граница (Operator Gateway)

Между **CascadeIDE (host)** и **remote client** — логический **Operator Gateway** (процесс или встроенный модуль):

1. **Исходящие события** — подмножество [0099](0099-ide-databus-typed-events-and-projections.md) / проекций ([0045](0045-agent-chat-persistence-event-log-and-projections.md)): сериализованный DTO, без секретов и без полных путей к `.env` / `ai-keys.toml`.
2. **Входящие команды** — узкий allowlist (`operator.reply`, `operator.approve`, `operator.reject`, `operator.pause_agent`, …), маппинг на существующие контуры IDE (Intercom, PFD confirmations), **не** прямой произвольный `ide_execute_command` с интернета.
3. **Транспорт:** предпочтительно **WebSocket или SSE + REST** на `localhost` с **TLS** и reverse proxy только при явной настройке; default — **loopback** + pairing token.
4. **Паритет восстановления** — по духу [0043](0043-mcp-transport-recovery-human-agent-parity.md): remote client при обрыве показывает «сессия недоступна», не симулирует успех.

**Связь с [0108](0108-web-ai-portal-host-object-tools-bridge.md):** Web AI Portal — **чужой origin** в WebView2; ROS-PWA — **свой** origin gateway, отдельный allowlist. Не смешивать портал и пульт.

### 2.4. Отличие от мультиоконности [0017](0017-multi-window-workspace-and-agent-surfaces.md)

| | ADR 0017 | ADR 0117 |
|---|----------|----------|
| Устройства | один ПК, N мониторов | N устройств, 1 «тяжёлая» станция |
| UI | Avalonia `TopLevel` | **PWA** (канон) |
| Синхронизация | общий процесс, общий `DataContext` | сеть / loopback gateway |
| Критерий | раскладка кокпита | наблюдение + решения вне кабины |

Оба могут работать **одновременно** (второй монитор + телефон как пульт).

### 2.5. Критерий Friction (негативный сценарий)

Считаем ROS успешным для сценария **S**:

1. Оператор отошёл от рабочей станции; на телефоне открыт пульт (или PWA).
2. Агент в IDE запросил уточнение или подтверждение.
3. Оператор **видит** запрос в течение разумного TTL и может **ответить** или **отклонить** без установки полного CIDE на телефон.
4. Агент **продолжает** на десктопе; в логе аудита есть связка `operator_device_id` + действие.

Если для S доступен только «открой ноутбук с IDE» — ROS **не** снял трение для этого класса задач.

---

## 3. Фазы (дорожная карта)

| Фаза | Содержание | Критерий готовности |
|------|------------|---------------------|
| **0** | Этот ADR; перечень DTO и allowlist в чертеже | Согласованы границы с 0017/0108/0031 |
| **1** | Read-only: gateway + **PWA** (manifest, service worker shell); snapshot + SSE | PWA на loopback: статус и «ждёт оператора» |
| **2** | Write + pairing; installable PWA на телефоне в LAN | Сценарий S проходит |
| **3** | Web Push / адаптеры; опционально TLS + VPN до gateway | Оператор вне LAN с защищённым каналом |

**MVP-ноль (вне кода CIDE):** async через Issues / org Discussions / Telegram-relay MCP — **не** заменяет ROS, допустим как временный обход.

---

## 4. Безопасность и доверие

- **Секреты** не передаются на remote client; `ai-keys.toml` и MCP-токены остаются на станции IDE.
- **Pairing:** короткоживущий код; список отозванных устройств; по умолчанию gateway слушает **127.0.0.1**.
- **Allowlist команд** на gateway — отдельный от [0108](0108-web-ai-portal-host-object-tools-bridge.md) и от полного MCP.
- **Аудит:** каждое remote-действие логируется с `principal=human`, `channel=remote_operator`, `device_id`.
- **Не** экспонировать gateway в интернет без явной политики org (VPN, mTLS).

---

## 5. Альтернативы (отклонены или отложены)

| Альтернатива | Почему не целевой путь |
|--------------|------------------------|
| **Полный web IDE** | Паритет Roslyn/отладки; огромное трение; дублирует cockpit |
| **Только RDP/VNC на десктоп** | Работает, но не mobile-friendly; не снижает трение сценария S |
| **Публичный MCP endpoint** без слоёв | Риск утечки workspace; см. backlog remote MCP в agent-notes |
| **Единый чат-бот без привязки к IDE** | Нет истины сессии; галлюцинация «я сделал» |
| **Нативный companion (v1)** | Два контура поддержки; PWA закрывает сценарий S; native — при доказанной необходимости |

---

## 6. Последствия

- Появится новый **feature slice** (например `Features/OperatorGateway/`): host service, DTO, allowlist, **статика PWA** (`manifest.webmanifest`, service worker, UI).
- Потребуются **тесты контракта** snapshot для wire-DTO ([0008](0008-mcp-contracts-and-testable-infrastructure.md), [0052](0052-agent-contract-cli-and-snapshot-tests.md)).
- **Intercom / 0031 / 0042:** remote approve должны сходиться с теми же машинами состояний, что PFD/MFD на десктопе — не второй параллельный «источник правды».
- Документация оператора: отдельная страница в handbook/kb-public **после** фазы 1 (не в этом ADR).

---

## 7. Открытые вопросы

1. **Встроенный gateway** vs отдельный процесс `cascade-operator-gateway` (проще обновлять, сложнее deploy)?
2. **Один workspace — несколько remote clients** (семья устройств) или один активный пульт?
3. **Связь с ACP [0016](0016-agent-client-protocol-external-agent.md):** remote surface управляет только встроенным агентом IDE или также внешним ACP?
4. **Офлайн:** очередь команд на клиенте vs жёсткий отказ?
5. Пересечение с **[0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md)** (emergency / presence) — общий канал уведомлений или отдельный?
6. **Web Push:** VAPID и доставка через gateway vs только внешний адаптер (Telegram) в фазе 2?

---

## 8. Статус реализации

| Область | Состояние |
|---------|-----------|
| ADR / границы | этот документ |
| Operator Gateway в коде | нет |
| PWA (manifest + SW + UI) | нет |
| Pairing | нет |
| Интеграция DataBus / Intercom | нет |

При появлении кода — обновить §8 и статус ADR на **Accepted · Implemented** (по [status-lifecycle.md](status-lifecycle.md)).
