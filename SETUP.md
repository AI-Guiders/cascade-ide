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

Команды (после установки Ollama):

```powershell
ollama pull llama3.2
ollama pull qwen2.5:7b
```

Список установленных моделей в CascadeIDE подтягивается автоматически при запуске.

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
