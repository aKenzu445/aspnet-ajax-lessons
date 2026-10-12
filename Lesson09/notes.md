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
