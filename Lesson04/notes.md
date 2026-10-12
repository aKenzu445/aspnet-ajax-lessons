# Занятие 4 — заметки и выводы

## Задача 1. «Пустой» заголовок страницы

**До:** в `_Layout.cshtml` (занятие 3) уже было `<title>@ViewData["Title"] — Task Manager</title>`, но значение никто не задавал. View Page Source:
```
/      → <title> — Task Manager</title>
/Tasks → <title> — Task Manager</title>
```
`ViewData["Title"]` было `null`, и Razor вывел пустоту перед тире.

**После:** в начале страниц добавлен блок `@{ ViewData["Title"] = "..."; }`:
```
/      → <title>Главная — Task Manager</title>
/Tasks → <title>Задачи — Task Manager</title>
```
**Почему это работает:** `ViewData` — словарь (`Dictionary<string, object>`) одного запроса, общий для страницы и её layout. Страница выполняется **раньше** layout, поэтому значение, записанное в странице, layout уже видит.
