# Занятие 3. Razor-синтаксис и Layout-страницы

Продолжение проекта **Lesson1.WebServer**: сайт стал многостраничным, общий layout, серверная отрисовка.
Выводы по задачам — в [`notes.md`](notes.md).

## Как запустить
Rider → `AjaxLessons.slnx` → **Занятие 3** → **Lesson1.WebServer** → **Run**. Адрес: **http://localhost:5103**.

## Структура Razor Pages

| Файл | Назначение |
|---|---|
| `Pages/_ViewStart.cshtml` | Layout по умолчанию для всех страниц |
| `Pages/_ViewImports.cshtml` | Общие `@using`, `@namespace`, подключение тег-хелперов |
| `Pages/Shared/_Layout.cshtml` | Каркас: шапка, меню (с подсветкой), футер, `@RenderBody()`, `@RenderSection("Scripts")` |
| `Pages/Shared/_TaskItem.cshtml` | Partial view: одна строка списка задач |
| `Pages/Index.cshtml` | Главная: инфо о сервере, сумма, echo, деление |
| `Pages/Tasks.cshtml` + `.cs` | Список задач: SSR через `TasksModel.OnGet()` + AJAX для изменений |
| `Pages/About.cshtml` + `.cs` | ДЗ: «О сервере» без AJAX |
| `Pages/Error.cshtml` + `.cs` | Страница ошибки для сайта |
| `Pages/CrashTestPage.cshtml` + `.cs` | Тест: страница, которая падает (`/crash-test-page`) |
| `Pages/Test.cshtml` | Задача 1: первая Razor-страница (`/Test`) |
| `Data/ServerInfo.cs` | Имя, версия и время запуска сервера |

## Задачи

| № | Задача | Где | Как проверить |
|---|---|---|---|
| 1 | Подключить Razor Pages | `Program.cs` (`AddRazorPages`, `MapRazorPages`), `Pages/Test.cshtml` | `/Test` → «Razor работает!» |
| 2 | `index.html` → `Pages/Index.cshtml` | `Pages/Index.cshtml` | `/` показывает прежний интерфейс, AJAX работает |
| 3 | Общий Layout | `Pages/Shared/_Layout.cshtml`, `Pages/_ViewStart.cshtml` | Шапка «Task Manager» и футер с годом на всех страницах |
| 4 | Страница `/Tasks` | `Pages/Tasks.cshtml`, меню в layout | Меню «Главная» / «Задачи», на `/Tasks` всё работает |
| 5 | PageModel и SSR | `Pages/Tasks.cshtml.cs` | Список виден сразу, даже без JS |
| 6 | Partial view | `Pages/Shared/_TaskItem.cshtml` | Строки из Razor и из JS выглядят одинаково |
| 7 | Секции Scripts | `@section Scripts` в Index/Tasks, `@RenderSection` в layout | View Page Source: скрипты только на своих страницах |
| 8 | Активный пункт меню | `_Layout.cshtml`, `ActiveClass()` | Подсветка переключается |
| 9 | Страница ошибки | `Pages/Error.cshtml`, `UseExceptionHandler` в `Program.cs` | `/crash-test-page` → HTML, `/api/crash-test` → JSON |
| 10 | «Задач пока нет» | `Pages/Tasks.cshtml` (`@if/else`), JS в `loadTasks` | Пустой список → сообщение; после добавления пропадает без F5 |
| ДЗ | «О сервере» без AJAX | `Pages/About.cshtml` + `.cs` | `/About`: имя, версия, время запуска, текущее время |

## Финальные тесты (проверено ✅)

| № | Действие | Результат |
|---|---|---|
| 1 | Открыть `/` | Шапка/футер из layout, калькулятор и echo работают ✅ |
| 2 | Меню → `/Tasks` | Список виден сразу (OnGet + partial `_TaskItem`) ✅ |
| 3 | View Page Source `/` и `/Tasks` | Скрипты только на своих страницах ✅ |
| 4 | Добавить задачу на `/Tasks` | Появляется без перезагрузки ✅ |
| 5 | Переключать страницы | Активный пункт подсвечивается ✅ |
| 6 | Очистить список и обновить `/Tasks` | «Задач пока нет, добавьте первую!» ✅ |
| 7 | `/crash-test-page` | Pages/Error.cshtml ✅ |
| 8 | `GET /api/crash-test` | JSON-ошибка, как в занятии 2 ✅ |
