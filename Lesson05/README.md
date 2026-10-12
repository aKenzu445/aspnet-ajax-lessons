# Занятие 5. Entity Framework Core: подключение базы данных

Продолжение проекта **Lesson1.WebServer**. Список задач в памяти заменён на базу данных **SQLite** через **EF Core** (подход Code-First). Данные сохраняются после перезапуска.
Подробности, выводы и вывод команд — в [`notes.md`](notes.md).

## Как запустить
Rider → `AjaxLessons.slnx` → **Занятие 5** → **Lesson1.WebServer** → **Run**. Адрес: **http://localhost:5105**.
База `tasks.db` создаётся/обновляется **автоматически** при запуске (`db.Database.Migrate()` в `Program.cs`). Вручную то же делает команда `dotnet ef database update` в папке `Lesson05`.

## Новое в проекте

| Файл | Что внутри |
|---|---|
| `Data/TaskDbContext.cs` | `DbContext` с таблицей `DbSet<TaskItem> Tasks` |
| `Models/TaskItem.cs` | Сущность: `Id` (ключ), `Title`, `IsDone`, `CreatedAt` (задача 11), `Category` (ДЗ) |
| `Migrations/..._InitialCreate.cs` | Миграция 1: создать таблицу `Tasks` |
| `Migrations/..._AddCreatedAt.cs` | Миграция 2: столбец `CreatedAt` |
| `Migrations/..._AddCategory.cs` | Миграция 3 (ДЗ): столбец `Category` |
| `appsettings.json` | `ConnectionStrings:TaskDb = "Data Source=tasks.db"` |
| ~~`Data/TaskStorage.cs`~~ | **Удалён** (задача 10) |

## Команды EF Core (в терминале Rider, папка `Lesson05`)

| Команда | Что делает |
|---|---|
| `dotnet tool install --global dotnet-ef` | Установить инструмент `dotnet ef` (один раз) |
| `dotnet ef --version` | Проверить версию |
| `dotnet ef migrations add ИмяМиграции` | Создать файл миграции (базу **не** меняет) |
| `dotnet ef database update` | Применить миграции к базе |
| `dotnet ef migrations remove` | Удалить последнюю (ещё не применённую) миграцию |

## Задачи

| № | Задача | Где | Как проверить |
|---|---|---|---|
| 1 | Данные пропадают при перезапуске | `notes.md` | Было: после перезапуска только 3 стартовые задачи |
| 2 | Пакеты EF Core + `dotnet-ef` | `Lesson1.WebServer.csproj` | `dotnet ef --version` → 10.0.11 |
| 3 | Сущность и `TaskDbContext` | `Models/TaskItem.cs`, `Data/TaskDbContext.cs` | Проект компилируется |
| 4 | Строка подключения + DI | `appsettings.json`, `AddDbContext` в `Program.cs` | Сервер стартует, `tasks.db` ещё нет |
| 5 | Миграция `InitialCreate` | `Migrations/` | `Up()` → `CreateTable("Tasks")`, `Down()` → `DropTable`; появился `tasks.db` |
| 6 | Просмотр базы | Rider → Database → SQLite → `tasks.db` | Таблица `Tasks`: Id (PK, автоинкремент), Title, IsDone |
| 7 | GET через EF | `Program.cs` → `ToListAsync`, `FindAsync` | Сначала `[]` и 404 — данных в базе нет |
| 8 | POST через EF | `Program.cs` → `Add` + `SaveChangesAsync` | 201, затем GET /1 → 200 |
| 9 | DELETE через EF | `Program.cs` → `Remove` + `SaveChangesAsync` | 200, затем GET → 404 |
| 10 | Razor-страницы на EF, `TaskStorage` удалён | `Pages/Tasks.cshtml.cs`, `Pages/Stats.cshtml.cs` (DI в конструкторе) | Перезапуск → задачи на месте |
| 11 | Миграция `AddCreatedAt` | `TaskItem.CreatedAt`, `_TaskItem.cshtml` | Новый столбец, старые задачи сохранились, дата на `/Tasks` |
| ДЗ | `Category` + фильтр LINQ | миграция `AddCategory`, `OnGetAsync(string? category)` | `/Tasks?category=Работа` → только «Работа» |

## Финальные тесты (проверено ✅)

| № | Действие | Результат |
|---|---|---|
| 1 | Добавить задачу через AJAX-форму | Сохраняется в `tasks.db`, видна дата создания ✅ |
| 2 | Добавить через классическую форму | Сохраняется, редирект и TempData-сообщение работают ✅ |
| 3 | Перезапустить сервер | Все задачи и их `CreatedAt` на месте ✅ |
| 4 | Удалить задачу через API и через интерфейс | Пропадает из базы и из интерфейса ✅ |
| 5 | `/Stats` | Считается по данным из базы (`CountAsync`) ✅ |
| 6 | Папка `Migrations` | `InitialCreate`, `AddCreatedAt` (+ `AddCategory` из ДЗ) ✅ |
| 7 | `tasks.db` в просмотрщике | Столбцы `Id, Title, IsDone, CreatedAt` (+ `Category`) ✅ |
