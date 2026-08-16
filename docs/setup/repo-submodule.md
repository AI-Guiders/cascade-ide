# Создание репо и подключение как submodule

**Историческая справка:** первый push `cascade-ide` был на self-hosted GitLab (RuVDS, группа Krawler). Инстанс **снят с эксплуатации**; канон — [github.com/AI-Guiders/cascade-ide](https://github.com/AI-Guiders/cascade-ide).

## Текущий канон

Submodule URL в `open/.gitmodules`:

```text
https://github.com/AI-Guiders/cascade-ide.git
```

Bootstrap:

```powershell
cd "d:\Experiments\Personal Cursor Folder\Financial\software\open"
git submodule update --init cascade-ide
```

## Добавить новый сиблинг (шаблон)

1. Создай репозиторий на GitHub (org `AI-Guiders` или личный).
2. Запушь код в новый remote.
3. Из корня `open`:

```powershell
git submodule add https://github.com/AI-Guiders/<repo>.git <path>
git add .gitmodules <path>
git commit -m "chore: add <repo> submodule"
```
