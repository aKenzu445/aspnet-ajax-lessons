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
