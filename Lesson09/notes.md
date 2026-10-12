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
