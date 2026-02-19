# Создание репо на GitLab и подключение как submodule

**Выполнено:** репо `Krawler/cascade-ide` создан на GitLab (при первом push), код запушен, в репо `open` добавлен submodule `cascade-ide`. Ниже — шаги для справки или повторения.

## 1. Создать пустой проект на GitLab

- Зайди на свой GitLab (например `http://193.124.113.7/Krawler`).
- **New project** → **Create blank project**.
- Имя: **cascade-ide**, видимость по желанию. Не инициализируй с README (репо должен быть пустым).
- Скопируй URL репо, например: `http://193.124.113.7/Krawler/cascade-ide.git`.

## 2. Запушить текущий код в новый репо

Из **корня workspace** (не из open):

```powershell
cd "d:\Experiments\Personal Cursor Folder\Financial\software\open\cascade-ide"
git init
git add .
git commit -m "Initial: Avalonia IDE + Ollama (CascadeIDE)"
git branch -M main
git remote add origin http://193.124.113.7/Krawler/cascade-ide.git
git push -u origin main
```

(Подставь свой URL и при необходимости логин/токен для push.)

## 3. Подключить cascade-ide как submodule в репо open

Сейчас `cascade-ide` — обычная папка внутри open. Нужно заменить её на submodule.

Из **корня workspace**:

```powershell
cd "d:\Experiments\Personal Cursor Folder\Financial\software\open"
# Удалить папку (код уже в отдельном репо)
Remove-Item -Recurse -Force cascade-ide
# Добавить submodule
git submodule add http://193.124.113.7/Krawler/cascade-ide.git cascade-ide
git add .gitmodules cascade-ide
git commit -m "Add cascade-ide as submodule"
git push
```

После этого в open будет сабмодуль `cascade-ide`, указывающий на твой GitLab-репо.
