# ADR 0195: Voice input — harness ear (PTT / VAD), не «слушай N секунд» в ходе агента

**Статус:** Accepted  
**Дата:** 2026-07-27  
**Tags:** #cdp #sense #voice #ptt #vad #whisper #webcam #harness #autonomy #kokoro #openvoice #adr #cascade-ide

## Связанные ADR

| ADR | Роль |
|-----|------|
| [0185](0185-life-thread-delayed-self-wake.md) | Wake / enqueue completion после локального события (utterance ready) |
| [0184](0184-harness-channel-mute-earplugs-cockpit.md) | Mute / «беруши» — голос-вход тоже должен уважать mute |
| [0187](0187-cdp-mcp-scene-agent-outlet.md) | Outlet ≠ in-proc sense; ухо не парковать в guest MCP |
| [0166](0166-agent-centric-harness-model-comfort-and-pay-per-token-economics.md) | Не жечь токены на «агент слушает»; ear вне FM |
| [0048](0048-cursor-acp-chat-ide-parity-and-mcp-tool-surface.md) | Исторический Cursor MCP surface (webcam / kokoro) |
| [0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md) | Opt-in сенсоры; privacy / presence — соседний контур |
| [0181](0181-mcp-imagecontent-agent-vision-opt-in.md) | Sense payload в ход — opt-in, не всегда vision/audio dump |

## Резюме

- Паттерн Cursor «скажи *слушай 10с* → агент пишет mic → Whisper → отвечает Kokoro» — **костыль модели хода**, не дефект микрофона.
- В харнессе (CDP / CIDE) **ухо живёт в локальном органе**: Push-to-talk (P0) или VAD (P1) → in-proc `audio` + `transcribe` → в ход агента уже **текст** (опционально path к wav).
- Агент **не** держит continuous listen между тиками и **не** спрашивает длительность у оператора.
- Sense stack in-proc (`cdp_webcam`): запись/Whisper уже есть (V0 autonomy); TTS out — отдельный контур (цель OpenVoiceTts; Kokoro interim).

---

## Контекст

### Как было в Cursor (до CDP desk)

Оператор хотел сказать агенту голосом. Единственный рабочий контракт:

1. В чате: «слушай, 10 секунд» (или аналог).
2. Агент вызывает MCP `capture_audio` / WaveIn на фиксированный `duration_sec`.
3. `transcribe_audio_whisper` → текст в контекст.
4. Ответ — часто через `kokoro-tts-mcp`.

Это работало, но UX плохой: надо заранее угадать длительность; тишина в конце окна; нельзя естественно «договорить мысль».

### Почему так вышло

Composer / ACP-агент живёт **тиками**: после конца хода нет фонового процесса «уха». Continuous listen внутри FM-тика = privacy + CPU + всё равно обрыв на end-of-turn. Поэтому «N секунд» было вынужденным API хода, а не архитектурой sense.

### Что изменилось (V0 sense desk)

In-proc `cdp_webcam` / `go=webcam_desk` (ship **0.5.233**): `audio`, `transcribe`, `av`, … — capture/analysis parity без outlet guest. Автономия ARM (`cdp_ignite`) будит *агента*, но **не заменяет** голосовой вход оператора.

Нужно зафиксировать, **где** живёт ухо в своём харнессе.

## Решение

### Разделение ролей

| Слой | Ответственность |
|------|-----------------|
| **Harness ear** (орган desk / hotkey / UI) | Старт/стоп записи, VAD, mute, privacy gate |
| **Sense primitives** (`cdp_webcam` op=audio\|transcribe\|av) | WaveIn / Whisper in-proc — вызываются органом, не «из промпта длительности» |
| **Wake / inject** ([0185](0185-life-thread-delayed-self-wake.md), `cdp_ignite`) | После готового utterance — enqueue ход с **текстом** (+ optional `audio_path`) |
| **Voice out** | Отдельный organ (цель: OpenVoiceTts; Kokoro — interim, не канон) |

### Канон входа (порядок внедрения)

**P0 — Push-to-talk (PTT)**  
- Hotkey / кнопка desk: *down* → начать запись, *up* → stop.  
- Орган: `op=audio` с duration = пока зажато (или start/stop API), затем `op=transcribe`.  
- Результат: `transcript` (+ `file_path`) → inject в Intercom / Composer / CCR как user message (или structured utterance event).  
- Агент отвечает текстом; TTS out — по политике канала (не обязателен в том же тике).

**P1 — Voice activity (VAD)**  
- Орган armed/disarmed явно (default **disarmed**; уважает [0184](0184-harness-channel-mute-earplugs-cockpit.md)).  
- Речь → буфер; тишина ≥ порога → конец фразы → тот же pipeline, что PTT.  
- Без «скажи длительность».

**P2 — Wake word (опционально)**  
- Поверх VAD: активация только после ключевой фразы. Не блокер P0/P1.

### Контракт utterance (черновик)

```text
utterance_ready:
  transcript: string
  audio_path?: path under workspace   # optional; агент может не читать wav
  language?: string
  source: "ptt" | "vad" | "wake"
  started_utc / ended_utc
  device_number?: int
```

В ход агента по умолчанию кладём **transcript**; сырой audio — по opt-in (аналог vision opt-in [0181](0181-mcp-imagecontent-agent-vision-opt-in.md)).

### Cursor dogfood (escape)

Пока пилотим в Cursor Composer: допускается legacy «слушай Nс» как **fallback** (нет PTT UI). Не считать это каноном харнесса. Цель dogfood — проверить sense primitives; канон UX — PTT/VAD в CDP/CIDE.

### Privacy / safety

- Mic: явный armed / PTT; нет always-on без opt-in.
- Mute канала ([0184](0184-harness-channel-mute-earplugs-cockpit.md)) глушит ear.
- Не путать с presence/liveness webcam ([0034](0034-pilot-incapacitation-emergency-mode-and-presence-sensing.md)) — другой контракт.

## Последствия

- UX: естественная речь без угадывания секунд.
- Экономика: Whisper/TTS локально; в FM только текст utterance → меньше токенов, чем «агент крутит audio tool вслепую».
- Архитектура: sense остаётся in-proc desk organ; ear — orchestration над ним + wake.
- Нужна реализация: hotkey/PTT UI + start/stop (или duration-from-hold) + VAD later; wire в ignite/Intercom.

## Отклонённые альтернативы

| Вариант | Почему нет |
|---------|------------|
| Continuous listen внутри агентского хода / FM | Нет фонового уха между тиками; privacy; обрыв на end-of-turn |
| Оставить «слушай Nс» каноном в CIDE | Плохой UX; костыль Cursor, не цель харнесса |
| Снова outlet webcam-*/kokoro MCP как primary ear/mouth | Против in-proc sense desk; Kokoro не целевой TTS |
| Always-on VAD без armed | Ложные срабатывания; privacy |

## Связь с V0 webcam autonomy

Flight `flight-webcam-autonomy-v0`: in-proc ops `frame|burst|av|screen|audio|transcribe|ocr|analyze` ship **0.5.233**. Этот ADR — **следующий слой**: не новый sense op, а **как оператор говорит в контур**, используя уже зелёный `audio`/`transcribe`.

## История изменений

<a id="adr0195-history"></a>

| Дата | Изменение |
|------|-----------|
| 2026-07-27 | Accepted: PTT P0 / VAD P1; ear вне FM; Cursor N-sec = fallback only |
