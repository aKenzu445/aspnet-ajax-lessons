# Занятие 8. EF Core, часть 2: связи таблиц, LINQ-запросы и пагинация

Продолжение проекта **Lesson1.WebServer**. Категории вынесены в отдельную таблицу (связь «один-ко-многим»), добавлены фильтр, сортировка, пагинация, статистика по категориям и поиск.
Подробные выводы, найденные «ловушки» и проверки — в [`notes.md`](notes.md).

## Как запустить
Rider → `AjaxLessons.slnx` → **Занятие 8** → **Lesson1.WebServer** → **Run**. Адрес: **http://localhost:5108**. База создаётся автоматически, три категории уже есть (HasData).

## Новое в проекте

| Файл | Что внутри |
|---|---|
| `Models/Category.cs` | Сущность: `Id`, `Name`, навигация `List<TaskItem> Tasks` |
| `Models/TaskItem.cs` | `int? CategoryId` (внешний ключ) + `Category? Category` (навигация) вместо строки |
| `Data/TaskDbContext.cs` | `DbSet<Category>`, `OnModelCreating`: `HasData` (3 категории) и `OnDelete(SetNull)` |
| `Pages/Categories.cshtml` + `.cs` | Список, добавление (с проверкой дублей), удаление категорий |
| `Pages/Tasks.cshtml` + `.cs` | `Include`, фильтр, сортировка, пагинация, поиск |
| `Pages/Stats.cshtml.cs` | `GroupBy` по категориям |
| `Migrations/..._AddCategories.cs` | Новая таблица, FK, seed + **перенос старых строковых категорий** |
| `Migrations/..._ChangeCategoryDeleteBehavior.cs` | `ON DELETE SET NULL` |
| ~~`TaskCategories.cs`, `AllowedCategoryAttribute.cs`~~ | Удалены: их заменила таблица и внешний ключ |

## LINQ в этом занятии

| Метод | Где | SQL |
|---|---|---|
| `.Include(t => t.Category)` | `TasksModel` | `LEFT JOIN Categories` |
| `.Where(t => t.CategoryId == id)` | фильтр | `WHERE` |
| `.Where(t => t.Title.Contains(s))` | поиск (ДЗ) | `WHERE Title LIKE '%s%'` |
| `.OrderBy(...)` / `.OrderByDescending(...)` / `.ThenBy(...)` | сортировка | `ORDER BY` |
| `.Skip(n).Take(5)` | пагинация | `LIMIT 5 OFFSET n` |
| `.CountAsync()` | число задач / страниц | `COUNT(*)` |
| `.GroupBy(t => t.Category!.Name).Select(g => new { g.Key, Count = g.Count() })` | `/Stats` | `GROUP BY ... COUNT(*)` |
| `.AnyAsync(c => c.Id == id)` | проверка категории в API | `EXISTS` |

## Задачи

| № | Задача | Где | Как проверить |
|---|---|---|---|
| 1 | Проблемы строковых категорий | `notes.md` | 4 проблемы и вывод |
| 2 | `Category` + связь | `Models/`, `TaskDbContext` | Компилируется |
| 3 | Миграция + HasData | `Migrations/..._AddCategories.cs` | В базе 3 категории без ручных INSERT, столбец `CategoryId` |
| 4 | Страница категорий | `/Categories` | Добавить «Спорт» → появилась |
| 5 | Выпадающий список категорий | формы `/Tasks`, `/Tasks/Edit/{id}` | В базе правильный `CategoryId` |
| 6 | Категория не видна → `Include` | `TasksModel.LoadPageDataAsync` | Названия категорий у задач |
| 7 | Фильтр | `/Tasks?categoryId=2` | Только «Учёба» |
| 8 | GroupBy | `/Stats` | Сумма по категориям = общему числу |
| 9 | Пагинация | `/Tasks?pageNumber=N` | 5 на страницу, края, страница 99 без ошибки |
| 10 | Сортировка | `/Tasks?sortBy=title` | Параметры не сбрасывают друг друга |
| 11 | Удаление категории | `/Categories` → «Удалить» | Было: FK error 500. Стало: задачи «без категории» |
| ДЗ | Поиск | `/Tasks?search=...` | Работает вместе с фильтром, сортировкой и страницами |

## Финальные тесты (проверено ✅)

| № | Действие | Результат |
|---|---|---|
| 1 | `/Categories` | Все категории, можно добавить новую ✅ |
| 2 | Создать задачу с категорией | Корректный `CategoryId` в базе ✅ |
| 3 | `/Tasks` | Название категории у задач (Include) ✅ |
| 4 | Фильтр «Учёба» | Только задачи этой категории ✅ |
| 5 | Сортировка «По названию» с фильтром | Фильтр сохраняется, порядок меняется ✅ |
| 6 | Листать страницы | Все три параметра работают вместе ✅ |
| 7 | `/Stats` | Разбивка по категориям ✅ |
| 8 | Удалить категорию с задачами | Задачи остаются «без категории», сервер не падает ✅ |
