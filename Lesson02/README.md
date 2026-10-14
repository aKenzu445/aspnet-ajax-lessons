# Занятие 2. Маршрутизация и Middleware

Продолжение проекта **Lesson1.WebServer** (копия занятия 1 + доработки занятия 2).
Выводы по задачам, которые нужно «зафиксировать письменно», — в [`notes.md`](notes.md).

## Как запустить
1. В Rider открой `AjaxLessons.slnx`, вверху справа выбери конфигурацию **Занятие 2** и нажми ▶ (или папка **Занятие 2** → правый клик по проекту **Lesson2.WebServer** → **Run**).
2. Сервер: **http://localhost:5102**. Логи запросов видны во вкладке **Run** внизу Rider.
3. Проверочные запросы: [`requests.http`](requests.http).

## Новые файлы

| Файл | Что внутри |
|---|---|
| `Middleware/ApiKeyMiddleware.cs` | Задача 7: проверка заголовка `X-Api-Key` для POST/PUT/DELETE `/api/tasks` |
| `Middleware/RateLimitingMiddleware.cs` | Домашнее задание: не больше 5 запросов к `/api` за 10 секунд с одного IP |
| `notes.md` | Наблюдения и выводы по задачам 1, 5, 6, 7, 9, 10 и ДЗ |

## Задачи

| № | Задача | Где в коде | Как проверить |
|---|---|---|---|
| 1 | Увидеть, что логов запросов нет | `notes.md` | Консоль сервера без middleware — только сообщения о запуске |
| 2 | Inline-middleware логирования | `Program.cs`, «задачи 2–3» (`app.Use`) | В консоли `[HH:mm:ss] → GET /index.html` |
| 3 | Код ответа и время | там же, `Stopwatch` | `← 400 (2 ms)` для `/api/divide?a=1&b=0` |
| 4 | `GET /api/tasks/{id}` | `Program.cs`, группа `tasksApi`, + блок «Найти задачу» на странице | `/api/tasks/1` → 200, `/api/tasks/99` → 404 |
| 5 | Route constraint `{id:int}` | `Program.cs`, `"/{id:int}"` | `/api/tasks/abc`: было 400 с исключением, стало 404 |
| 6 | Эксперимент с порядком middleware | `notes.md` | Статические файлы пропадают из лога, если логирование стоит после `UseStaticFiles` |
| 7 | `ApiKeyMiddleware` | `Middleware/ApiKeyMiddleware.cs`, `index.html` (`API_KEY`) | POST без ключа → 401, со страницы → 201 |
| 8 | Глобальная обработка ошибок | `Program.cs`, «задача 8» (первый `app.Use`), `/api/crash-test` | `/api/crash-test` → 500 `{ "error": "Внутренняя ошибка сервера" }` |
| 9 | `MapGroup("/api/tasks")` | `Program.cs`, `var tasksApi = ...` | Все запросы со страницы работают без изменений в JS |
| 10 | Режим техработ | `Program.cs`, «задача 10» | Заголовок `X-Maintenance: true` → 503 для `/api/*` |
| ДЗ | Rate limiting → 429 | `Middleware/RateLimitingMiddleware.cs` | 6-й запрос за 10 секунд → 429 |

## Финальные тесты (проверено ✅)

| № | Действие | Результат |
|---|---|---|
| 1 | Открыть index.html, смотреть консоль | Строка лога на каждый запрос, со статусом и временем ✅ |
| 2 | `GET /api/tasks/1` | 200, конкретная задача ✅ |
| 3 | `GET /api/tasks/abc` | 404, сработал route constraint ✅ |
| 4 | `POST /api/tasks` без `X-Api-Key` | 401 ✅ |
| 5 | Добавить задачу через страницу (ключ в JS) | 201, задача появляется в списке ✅ |
| 6 | `GET /api/crash-test` | 500, контролируемый JSON ✅ |
| 7 | `X-Maintenance: true` к `/api/*` | 503 ✅ |
| 8 | Калькулятор и echo занятия 1 | Работают как прежде ✅ |
