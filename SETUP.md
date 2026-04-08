# Установка стека CascadeIDE на машину

Краткий чеклист и рекомендации под твою конфигурацию.

## Характеристики машины (по systeminfo)

- **ОС:** Windows 11 Pro (10.0.26200)
- **Память:** 64 GB RAM
- **CPU:** AMD64 Family 25 Model 116 (~3.8 GHz)
- **GPU:** NVIDIA GeForce RTX 4060 Laptop GPU, AMD Radeon 780M

Под такой конфиг комфортно запускать модели 7B–13B в GPU и при необходимости крупнее — в CPU/RAM.

---

## 1. .NET 10 SDK

Уже установлен (10.0.103). Проверка:

```powershell
dotnet --list-sdks
```

Если нужно поставить: [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0).

---

## 2. Шаблоны Avalonia

Установка (один раз):

```powershell
dotnet new install Avalonia.Templates
```

Проверка: `dotnet new list` — в списке должны быть `avalonia.app`, `avalonia.mvvm` и др.

---

## 3. Ollama (локальные модели)

Без Ollama приложение запустится, но чат/модели недоступны.

**Установка под Windows:**

- Вариант 1 (PowerShell):
  ```powershell
  irm https://ollama.com/install.ps1 | iex
  ```
- Вариант 2: скачать [OllamaSetup.exe](https://ollama.com/download/windows) с сайта.

После установки сервис слушает **http://localhost:11434**. Первый запуск модели (например в терминале `ollama run llama3.2`) скачает модель и покажет её в CascadeIDE.

**Требования:** Windows 10 22H2+, для GPU — драйверы NVIDIA 452.39+ или актуальные AMD.

---

## 4. Рекомендуемые модели под твоё железо

| Класс   | Примеры моделей      | Комментарий |
|--------|------------------------|-------------|
| Быстрые (7B–8B) | `llama3.2`, `qwen2.5:7b`, `phi4` | Удобны для чата и подсказок по коду, хорошо помещаются в VRAM RTX 4060. |
| Средние (13B)  | `qwen2.5:13b`, `llama3.1:8b`     | При 8 GB VRAM часть может уйти в RAM — всё ещё приемлемо. |
| Крупные        | `qwen2.5:32b`, `llama3.1:70b`    | 64 GB RAM позволяют тянуть на CPU или гибрид; скорость ниже. |

**Для ноутбука + MCP (tool calling) и кодинга на .NET** по умолчанию в настройках подставлена модель **qwen2.5-coder:7b** — поддерживает вызов инструментов и заточена под код. Установка:

```powershell
ollama pull qwen2.5-coder:7b
```

Выбранная в IDE модель сохраняется в настройках (`%LocalAppData%\CascadeIDE\settings.toml`) и подставляется при следующем запуске.

Команды (после установки Ollama):

```powershell
ollama pull qwen2.5-coder:7b
ollama pull llama3.2
ollama pull qwen2.5:7b
```

Список установленных моделей в CascadeIDE подтягивается автоматически при запуске.

---

## 4.1. MCP: агент ↔ IDE

Чтобы агент (Cursor и др.) управлял CascadeIDE по MCP, IDE нужно запускать с флагом **`--mcp-stdio`**. Тогда она поднимает MCP-сервер на stdin/stdout; хост (например Cursor) подключается к этому процессу и вызывает тулы: открытие файла, брейкпоинты, превью, подтверждения.

Подробно и пример конфига Cursor: **[docs/MCP-PROTOCOL.md](docs/MCP-PROTOCOL.md)**.

---

## 5. Сборка и запуск CascadeIDE

Из каталога репо **open**:

```powershell
cd cascade-ide
dotnet restore
dotnet build
dotnet run
```

Окно покажет статус Ollama и список локальных моделей (или подсказку установить/запустить Ollama).
