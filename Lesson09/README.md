# Занятие 9. Проектирование REST API: DTO, сериализация и профессиональный AJAX-клиент

Продолжение проекта **Lesson1.WebServer**. Изменения касаются только API (`/api/*`) и его AJAX-клиента. Razor-страницы со своей логикой не менялись.
Выводы, эксперименты и вывод запросов — в [`notes.md`](notes.md).

## Как запустить
Rider → `AjaxLessons.slnx` → **Занятие 9** → **Lesson1.WebServer** → **Run**. Адрес: **http://localhost:5109**. Проверочные запросы — [`requests.http`](requests.http).

## Новые файлы

| Файл | Что внутри |
|---|---|
| `Dtos/TaskDto.cs` | Задача 2: DTO для ответа + ручной маппинг `FromEntity` |
| `Dtos/CreateTaskDto.cs` | Задача 3: DTO для создания (без Id/IsDone/CreatedAt) |
| `Dtos/UpdateTaskDto.cs` | Задача 4: DTO для полного обновления |
| `Dtos/CategoryDto.cs` | Задача 5: категория + число задач |
| `Dtos/PagedResultDto.cs` | Задача 8: «конверт» для постраничного ответа |
| `Api/ApiError.cs` | Задача 6 + ДЗ: единый формат ошибок, коды, поле с ошибкой |
| `wwwroot/js/api.js` | Задача 11 + ДЗ: `apiRequest()`, всплывающие ошибки, реакция на коды |

## API после занятия 9

| Метод и адрес | Тело | Успех | Ошибки |
|---|---|---|---|
| `GET /api/tasks?categoryId&search&sortBy&page&pageSize` | — | 200 `{ items: TaskDto[], page, pageSize, totalCount, totalPages }` | 400 |
| `GET /api/tasks/{id}` | — | 200 `TaskDto` | 404 `TASK_NOT_FOUND` |
| `POST /api/tasks` | `CreateTaskDto` | **201** + `Location` + `TaskDto` | 400 `VALIDATION_ERROR` (+ field), 401 |
| `PUT /api/tasks/{id}` | `UpdateTaskDto` | 200 `TaskDto` | 400, 401, 404 |
| `DELETE /api/tasks/{id}` | — | **204** без тела | 401, 404 |
| `GET /api/categories` | — | 200 `CategoryDto[]` | — |
| любой неизвестный `/api/...` | — | — | 404 `ROUTE_NOT_FOUND` |

Все ошибки: `{ "error": { "message": "...", "code": "..." } }` (для `VALIDATION_ERROR` ещё и `"field"`).

## Задачи

| № | Задача | Где | Как проверить |
|---|---|---|---|
| 1 | Циклическая сериализация | `notes.md` | `Include` + сущность → `JsonException: object cycle` |
| 2 | `TaskDto` | `Dtos/TaskDto.cs`, GET-endpoints | `GET /api/tasks/1` с `categoryName` |
| 3 | `CreateTaskDto`, overposting | `Dtos/CreateTaskDto.cs`, POST | `isDone: true, id: 999` игнорируются |
| 4 | `UpdateTaskDto` | `Dtos/UpdateTaskDto.cs`, PUT, чекбокс | PUT меняет название, статус, категорию |
| 5 | `CategoryDto` + `/api/categories` | `Program.cs` | Список с `taskCount` |
| 6 | Единый формат ошибок | `Api/ApiError.cs`, все endpoints и middleware | Одна структура у 400/401/404/500/503 |
| 7 | 201 + Location, 204 | POST, DELETE | Заголовок `Location: /api/tasks/N`, DELETE → 204 |
| 8 | Пагинация в API | `GET /api/tasks` | Конверт с метаданными |
| 9 | Индикатор загрузки | `Tasks.cshtml` (добавление, удаление, чекбокс) | Кнопка «Сохранение...», повторный клик невозможен |
| 10 | Живой поиск + AbortController | `Tasks.cshtml` → «Живой поиск» | Всегда результат последнего ввода |
| 11 | `apiRequest()` | `wwwroot/js/api.js` | Все AJAX-вызовы API через одну функцию |
| ДЗ | Коды ошибок → разное поведение | `reactToErrorCode()` в `api.js`, `field` в `ApiError` | Подсветка поля, особое сообщение для 401 |

## Финальные тесты (задача 12, проверено ✅)

| № | Действие | Результат |
|---|---|---|
| 1 | `GET /api/tasks/1` | Чистый `TaskDto` с `categoryName` ✅ |
| 2 | POST с `id`/`isDone` | Поля игнорируются ✅ |
| 3 | Заголовок `Location` у POST | `/api/tasks/N` ✅ |
| 4 | `GET /api/tasks?categoryId=1&sortBy=title&page=1&pageSize=5` | Отфильтровано, отсортировано, с метаданными ✅ |
| 5 | Любая ошибка | `{ "error": { "message", "code" } }` ✅ |
| 6 | «Добавить задачу» на медленном сервере | Кнопка заблокирована, повторный клик невозможен ✅ |
| 7 | Быстрый ввод в живом поиске | Актуальный результат, устаревшие запросы отменены ✅ |
| 8 | JS страниц | Все запросы к API через `apiRequest()` ✅ |
