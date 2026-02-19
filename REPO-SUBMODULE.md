# Создание репо на GitLab и подключение как submodule

**Выполнено:** репо `Krawler/agent-ide` создан на GitLab (при первом push), код запушен, в репо `open` добавлен submodule `agent-ide`. Ниже — шаги для справки или повторения.

## 1. Создать пустой проект на GitLab

- Зайди на свой GitLab (например `http://193.124.113.7/Krawler`).
- **New project** → **Create blank project**.
- Имя: **agent-ide**, видимость по желанию. Не инициализируй с README (репо должен быть пустым).
- Скопируй URL репо, например: `http://193.124.113.7/Krawler/agent-ide.git`.

## 2. Запушить текущий код в новый репо

Из **корня workspace** (не из open):

```powershell
cd "d:\Experiments\Personal Cursor Folder\Financial\software\open\agent-ide"
git init
git add .
git commit -m "Initial: Avalonia IDE + Ollama (agent-ide)"
git branch -M main
git remote add origin http://193.124.113.7/Krawler/agent-ide.git
git push -u origin main
```

(Подставь свой URL и при необходимости логин/токен для push.)

## 3. Подключить agent-ide как submodule в репо open

Сейчас `agent-ide` — обычная папка внутри open. Нужно заменить её на submodule.

Из **корня workspace**:

```powershell
cd "d:\Experiments\Personal Cursor Folder\Financial\software\open"
# Удалить папку (код уже в отдельном репо)
Remove-Item -Recurse -Force agent-ide
# Добавить submodule
git submodule add http://193.124.113.7/Krawler/agent-ide.git agent-ide
git add .gitmodules agent-ide
git commit -m "Add agent-ide as submodule"
git push
```

После этого в open будет сабмодуль `agent-ide`, указывающий на твой GitLab-репо.
