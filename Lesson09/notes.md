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
