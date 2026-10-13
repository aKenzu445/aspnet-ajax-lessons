# Занятие 9 — заметки и выводы

## Задача 1. Проблема циклической сериализации

В `GET /api/tasks` добавлен `.Include(t => t.Category)`, а ответ по-прежнему — сущности `TaskItem` напрямую.

**Результат:**
```
GET /api/tasks → 500 {"error":"Внутренняя ошибка сервера"}
В консоли сервера: System.Text.Json.JsonException: A possible object cycle was detected.
This can either be due to a cycle or if the object depth is larger than the maximum allowed depth of 64.
```
(500 с аккуратным JSON — это наш middleware обработки ошибок API из занятия 2. Без него клиент получил бы «сырую» ошибку.)

**Причина — двусторонняя навигационная связь:** `TaskItem.Category` → `Category.Tasks` (список задач категории, EF заполнил его теми же загруженными задачами) → снова та же задача → её `Category` → ... Сериализатор JSON идёт по свойствам вглубь и ходит по кругу. На глубине 64 он сдаётся с ошибкой.

Можно было бы включить `ReferenceHandler.Preserve`, но тогда JSON стал бы нечитаемым (`{"$id":"1","$values":[...]}`). Правильное решение — **DTO** (задача 2).

## Задача 2. TaskDto для чтения

`Dtos/TaskDto.cs`: `record TaskDto(Id, Title, IsDone, CreatedAt, CategoryId, CategoryName, DueDate)` + статический метод `FromEntity(TaskItem)`, явный ручной маппинг. Поля задания: `Id, Title, IsDone, CreatedAt, CategoryName`. Добавлены `CategoryId` и `DueDate`: они есть в модели после ДЗ, и `CategoryId` клиенту нужен, чтобы отправить его обратно в `PUT` (задача 4).

- `GET /api/tasks` → `Include` → `ToListAsync()` → `tasks.Select(TaskDto.FromEntity)`.
- `GET /api/tasks/{id}` → `Include(...).FirstOrDefaultAsync(t => t.Id == id)` (`FindAsync` не умеет `Include`) → `TaskDto.FromEntity(task)`.

**Результат:**
```json
GET /api/tasks/1 → 200
{"id":1,"title":"Изучить ASP.NET","isDone":false,"createdAt":"2026-10-13T00:05:18","categoryId":2,"categoryName":"Учёба","dueDate":null}
```
Без циклов и без `$id/$values`. Категория — **текстом** (`categoryName`), а не вложенным объектом.

## Задача 3. CreateTaskDto и защита от overposting

**Воспроизведение на текущей реализации:**
```
POST /api/tasks {"title":"Новая задача","isDone":true,"id":999,"createdAt":"2020-01-01"}
→ 201 {"id":4,"title":"Новая задача","isDone":false, "createdAt":"2026-10-13T00:05:58", ...}
```
Проблема **не воспроизвелась**. С занятия 1 `POST` принимает не сущность `TaskItem`, а отдельный `record CreateTaskRequest(Title, ...)`, по сути уже DTO. В нём не было полей `Id`/`IsDone`/`CreatedAt`, и лишние поля JSON сериализатор просто проигнорировал. Если бы параметр был `TaskItem request`, клиент смог бы задать `IsDone = true` и `CreatedAt` в прошлом (а `Id = 999` — попытку вставить свой ключ).
При этом **ответ** всё ещё был сущностью (`"category": null` — навигационное свойство «утекло» наружу).

**Сделано:**
- `Dtos/CreateTaskDto.cs`: `record CreateTaskDto(string? Title, int? CategoryId, DateTime? DueDate)`. `DueDate` — из ДЗ занятия 7.
- `POST /api/tasks` принимает `CreateTaskDto`. Сервер сам задаёт `IsDone = false`, `CreatedAt = DateTime.Now`, а `Id` выдаёт база.
- Ответ — `TaskDto`. Категория для `categoryName` подгружается **явной загрузкой** `db.Entry(created).Reference(t => t.Category).LoadAsync()`.
- Серверная валидация сохранена: `Validator.TryValidateObject` (занятие 7) проверяет задачу, созданную из `CreateTaskDto`. Правила остались в одном месте, в атрибутах `TaskItem`.

**Проверка:**
```
POST {"title":"Новая задача","isDone":true,"id":999,"createdAt":"2020-01-01","categoryId":3}
→ 201 {"id":5,"title":"Новая задача","isDone":false,"createdAt":"2026-10-13T00:06:18","categoryId":3,"categoryName":"Личное",...}
   IsDone = false (а не true), Id = 5 от базы (а не 999), дата — текущая
POST {"title":"аб"} → 400 {"errors":["От 3 до 100 символов"]}
```

## Задача 4. UpdateTaskDto: полное обновление через PUT

- `Dtos/UpdateTaskDto.cs`: `record UpdateTaskDto(string? Title, bool IsDone, int? CategoryId)`.
- `PUT /api/tasks/{id:int}` теперь меняет **название, статус и категорию** (раньше только `isDone`). Ответ — `TaskDto`.
- Проверка только изменяемых полей: `Validator.TryValidateProperty(title, new ValidationContext(task) { MemberName = "Title" }, results)` + существование категории. Проверять весь объект (`TryValidateObject`) здесь **нельзя**: у старой задачи срок мог уже пройти, и `[MustBeFutureOrToday]` не дал бы даже отметить её выполненной.
- **JS страницы `/Tasks`:** чекбокс «выполнено» отправляет полный набор `{ title, isDone, categoryId }`. Название и категорию он берёт из `data-title` / `data-category-id`, которые partial `_TaskItem` выводит у каждой строки.

**Проверка:**
```
PUT /api/tasks/2 {"title":"Изучить ASP.NET Core","isDone":true,"categoryId":2}
→ 200 {"id":2,"title":"Изучить ASP.NET Core","isDone":true,"categoryId":2,"categoryName":"Учёба",...}
PUT /api/tasks/2 {"title":"","isDone":true,"categoryId":77} → 400 {"errors":["Название обязательно","Категория не найдена"]}
PUT /api/tasks/999 → 404
Чекбокс на /Tasks у «Курсовая» → зачёркнута; GET /api/tasks/3 → isDone: true, название и категория не потерялись
```

## Задача 5. CategoryDto и GET /api/categories

- `Dtos/CategoryDto.cs`: `record CategoryDto(int Id, string Name, int TaskCount)`. Вместо списка задач — **количество**, чтобы снова не получить цикл.
- `GET /api/categories`:
  ```csharp
  db.Categories.OrderBy(c => c.Id)
      .Select(c => new CategoryDto(c.Id, c.Name, c.Tasks.Count()))   // проекция прямо в SQL
      .ToListAsync()
  ```
  `c.Tasks.Count()` EF переводит в подзапрос `SELECT COUNT(*)`. Задачи в память не загружаются, `Include` не нужен.

**Проверка:**
```
GET /api/categories → 200
[{"id":1,"name":"Работа","taskCount":0},{"id":2,"name":"Учёба","taskCount":3},{"id":3,"name":"Личное","taskCount":1}]
```

## Задача 6. Единый формат ошибок во всех endpoints

**Было** (занятия 1–8): `{ "error": "..." }` в одних местах, `{ "errors": [...] }` в других, пустой ответ 404 для неподошедшего маршрута, текст исключения для неверных параметров `/api/sum`.

**Стало:** статический класс `Api/ApiError.cs`. Все ошибки имеют форму
```json
{ "error": { "message": "Задача не найдена", "code": "TASK_NOT_FOUND" } }
```
Коды: `VALIDATION_ERROR`, `TASK_NOT_FOUND`, `INVALID_PARAMETERS`, `DIVISION_BY_ZERO`, `UNAUTHORIZED`, `TOO_MANY_REQUESTS`, `MAINTENANCE`, `ROUTE_NOT_FOUND`, `INTERNAL_ERROR`.

Что изменено:
- endpoints `/api/tasks` (GET/POST/PUT/DELETE), `/api/divide` → `ApiError.NotFound/BadRequest/Validation(...)`;
- middleware: 401 (`ApiKeyMiddleware`), 429 (`RateLimitingMiddleware`), 503 (техработы), 500 (обработчик ошибок) → `ApiError.WriteAsync(...)`;
- `/api/sum`, `/api/echo` и сломанный JSON в теле: Minimal API бросает `BadHttpRequestException`. Middleware ловит его → 400 `INVALID_PARAMETERS`. Чтобы так было в любом режиме, а не только в Development, включено `RouteHandlerOptions.ThrowOnBadRequest = true`;
- неизвестный адрес под `/api` (в том числе `/api/tasks/abc`) → `app.MapFallback("/api/{**path}", ...)` → 404 `ROUTE_NOT_FOUND`;
- JS страниц читает `data.error.message`.

**Проверка (сравнение разных endpoints):**
```
GET  /api/tasks/999        → 404 {"error":{"message":"Задача не найдена","code":"TASK_NOT_FOUND"}}
GET  /api/tasks/abc        → 404 {"error":{"message":"Такого адреса API не существует","code":"ROUTE_NOT_FOUND"}}
POST /api/tasks без ключа  → 401 {"error":{"message":"Неверный или отсутствующий API-ключ","code":"UNAUTHORIZED"}}
POST /api/tasks {"title":""} → 400 {"error":{"message":"Название обязательно","code":"VALIDATION_ERROR"}}
POST сломанный JSON        → 400 {"error":{"message":"Некорректные параметры запроса","code":"INVALID_PARAMETERS"}}
GET  /api/sum?a=5          → 400 {"error":{"message":"Некорректные параметры запроса","code":"INVALID_PARAMETERS"}}
GET  /api/divide?a=1&b=0   → 400 {"error":{"message":"Деление на ноль невозможно","code":"DIVISION_BY_ZERO"}}
GET  /api/crash-test       → 500 {"error":{"message":"Внутренняя ошибка сервера","code":"INTERNAL_ERROR"}}
X-Maintenance: true        → 503 {"error":{"message":"Сервер временно недоступен, ...","code":"MAINTENANCE"}}
```
Структура у всех одинаковая. На странице: «Ошибка 400: Деление на ноль невозможно».

## Задача 7. Ревизия REST-конвенций: 201 + Location, 204

- `POST /api/tasks`: `Results.Json(..., statusCode: 201)` → **`Results.Created($"/api/tasks/{created.Id}", dto)`**. Код тот же (201), но теперь есть заголовок **`Location`** — ссылка на созданный ресурс.
- `DELETE /api/tasks/{id}`: было **200** + `{ "message": "Задача №N удалена" }` → стало **`204 No Content`** (`Results.NoContent()`), без тела. JS удаления больше не читает `response.json()` при успехе (тела нет), а сообщение формирует сам.
- Остальные: `GET` → 200, `PUT` → 200 + обновлённый DTO (по конвенции допустим 200 с телом или 204), ошибки — 400/401/404/429/500/503 в едином формате (задача 6).

**Проверка:**
```
POST /api/tasks {"title":"Проверить Location","categoryId":1}
→ HTTP/1.1 201 Created
  Location: /api/tasks/6
  {"id":6,"title":"Проверить Location",...,"categoryName":"Работа"}
GET /api/tasks/6 (адрес из Location) → 200 та же задача
DELETE /api/tasks/6 → HTTP/1.1 204 No Content
```
В Rider HTTP Client заголовки ответа видны в панели результата (аналог вкладки Headers в Postman).

## Задача 8. Фильтрация, сортировка и пагинация в API

`GET /api/tasks?categoryId=&search=&sortBy=&page=&pageSize=`. Тот же LINQ, что на странице `/Tasks` (занятие 8): `Include → Where (категория, поиск) → CountAsync → OrderBy/ThenBy → Skip/Take`. Защита от бессмысленных значений: `page ≥ 1`, `pageSize` в диапазоне 1..50 (`Math.Clamp`), по умолчанию 10.

Ответ — «конверт» `Dtos/PagedResultDto.cs` (`record PagedResultDto<T>(Items, Page, PageSize, TotalCount, TotalPages)`; `<T>` — обобщённый тип, подойдёт для любых элементов):
```json
GET /api/tasks?categoryId=2&sortBy=title&page=2&pageSize=5 → 200
{ "items": [ "Учебная задача 4", ... "Учебная задача 8" (TaskDto) ],
  "page": 2, "pageSize": 5, "totalCount": 11, "totalPages": 3 }
```
Проверено: вторая страница отфильтрованных («Учёба») и отсортированных по названию задач, `totalPages = ceil(11 / 5) = 3`. `?page=99` → `items: []` без ошибки. Без параметров: `page 1, pageSize 10`.

Примечание: в Minimal API параметр **может** называться `page`. Конфликт со служебным значением маршрута `page` есть только у Razor Pages (занятие 8, задача 9).

## Задача 9. Индикатор загрузки для AJAX-запросов

Паттерн из теории 9.5 применён к трём действиям на `/Tasks`:
- **«Добавить задачу»:** `button.disabled = true; button.textContent = "Сохранение..."`, запрос в `try`, ошибка сети в `catch`, разблокировка в **`finally`**;
- **«Удалить»** (AJAX): кнопка передаётся в `deleteTask(id, this)`, на время запроса «Удаление...» и заблокирована;
- **чекбокс «выполнено»:** `checkbox.disabled = true` на время `PUT`.

`finally` выполняется **всегда**: после успеха, после ошибки и даже после `return` внутри `try`. Поэтому кнопка гарантированно разблокируется.

**Проверка с замедленным сервером** (временно `await Task.Delay(2000)` в начале `POST /api/tasks`, после проверки убрано):
```
Клик «Добавить задачу» → через 0,3 с: disabled = true, текст «Сохранение...»
Ещё два клика во время запроса → игнорируются (кнопка заблокирована)
Через 2,6 с: disabled = false, текст «Добавить задачу», «Задача №16 добавлена»
Добавлено задач: 1 (а не 3)
```

**Ошибка по ходу:** после правки на странице перестали работать все кнопки. В консоли браузера `Uncaught SyntaxError: Unexpected token '}'`: при переписывании функции осталась лишняя закрывающая скобка. Одна синтаксическая ошибка «убивает» весь `<script>`. Это снова (как в задании с фильмами) показывает: при странном поведении первым делом смотреть **консоль браузера**.

## Задача 10. Живой поиск и отмена устаревших запросов (AbortController)

- Отдельный endpoint не нужен: используется `GET /api/tasks?search=...` (задача 8, ДЗ занятия 8).
- На `/Tasks` поле «Живой поиск». На событие `input` (каждый символ) выполняется `liveSearch(текст)`:
  ```js
  if (currentController) currentController.abort();   // отменить предыдущий запрос
  currentController = new AbortController();
  const response = await fetch(url, { signal: currentController.signal });
  // ... при отмене fetch выбрасывает ошибку с name === "AbortError" — её просто игнорируем
  ```

**Проверка race condition** (временно сервер отвечал тем дольше, чем короче запрос: `Task.Delay(1500 - длина * 400)`; ввод «О» → «От» → «Отч» с интервалом 50 мс):

| | В поле | На экране результат для | Что показано |
|---|---|---|---|
| **Без** AbortController (abort временно отключён) | «Отч» | **«О»** ❌ | Отпуск, Отчёт по работе (устаревший ответ пришёл последним и затёр правильный) |
| **С** AbortController | «Отч» | **«Отч»** ✅ | Отчёт по работе (2 устаревших запроса отменены) |

Вывод: на экране всегда результат **последнего** введённого запроса, независимо от порядка ответов сервера. Задержка после проверки убрана.
