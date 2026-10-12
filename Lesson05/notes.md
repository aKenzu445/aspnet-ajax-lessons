# Занятие 5 — заметки и выводы

## Задача 1. Демонстрация проблемы: данные исчезают при перезапуске

Добавлены 2 задачи, затем сервер остановлен и запущен заново.

**До перезапуска** (`GET /api/tasks`):
```json
[{"id":1,"title":"Изучить ASP.NET"...},{"id":2,...},{"id":3,...},
 {"id":4,"title":"Задача до перезапуска 1"...},{"id":5,"title":"Задача до перезапуска 2"...}]
```
**После перезапуска:**
```json
[{"id":1,"title":"Изучить ASP.NET"...},{"id":2,...},{"id":3,...}]
```
Задачи 4 и 5 пропали, остались только 3 «стартовые» задачи, захардкоженные в `TaskStorage`.

**Причина:** список `List<TaskItem>` живёт в **оперативной памяти** процесса сервера. Когда процесс завершается, память освобождается, и все данные исчезают. При новом запуске список создаётся заново из кода. Это не баг конкретного места, а свойство хранения в памяти. **Решение:** хранить данные в базе данных на диске (задачи 2–10).

## Задача 2. Установка EF Core и провайдера SQLite

В Rider пакеты ставятся через **NuGet** (правый клик по проекту → Manage NuGet Packages) или командами в терминале:
```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.11
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.11
dotnet tool install --global dotnet-ef --version 10.0.11
```
- `Microsoft.EntityFrameworkCore.Sqlite` — сам EF Core и «провайдер» для SQLite (умеет переводить LINQ в SQL-диалект SQLite);
- `Microsoft.EntityFrameworkCore.Design` — нужен инструментам миграций во время разработки;
- `dotnet-ef` — консольная команда `dotnet ef ...` (аналог Package Manager Console из Visual Studio).

**Проверка:**
```
$ dotnet ef --version
Entity Framework Core .NET Command-line Tools
10.0.11
```
Пакеты записаны в `Lesson1.WebServer.csproj` (`<PackageReference ... />`).

## Задача 4. Строка подключения и регистрация DbContext в DI

- `appsettings.json` → `"ConnectionStrings": { "TaskDb": "Data Source=tasks.db" }`. **Строка подключения** описывает, где база и как к ней подключиться. Для SQLite это просто путь к файлу.
- `Program.cs`:
  ```csharp
  builder.Services.AddDbContext<TaskDbContext>(options =>
      options.UseSqlite(builder.Configuration.GetConnectionString("TaskDb")));
  ```
  Это регистрация в контейнере **внедрения зависимостей (DI)**. Теперь, если endpoint или PageModel попросит `TaskDbContext` (параметром или в конструкторе), ASP.NET Core сам его создаст и передаст.

**Проверка:** сервер запускается без исключений. Файла `tasks.db` ещё **нет** (`ls tasks.db` → No such file or directory): регистрация только *описывает*, как подключаться, но саму базу не создаёт.

## Задача 5. Первая миграция и создание базы данных

**Шаг 1 — создать миграцию** (в терминале Rider, в папке `Lesson05`):
```bash
dotnet ef migrations add InitialCreate
```
Появилась папка `Migrations`:
- `20261012203245_InitialCreate.cs` — сама миграция (методы `Up` и `Down`);
- `..._InitialCreate.Designer.cs` — служебное описание модели на момент миграции;
- `TaskDbContextModelSnapshot.cs` — «снимок» текущей модели. С ним EF сравнивает классы при следующей миграции.

После этой команды файла `tasks.db` **ещё нет**: миграция — только **план** изменений, она не меняет базу.

**Что внутри `Up()`** — создать таблицу:
```csharp
migrationBuilder.CreateTable(
    name: "Tasks",
    columns: table => new
    {
        Id = table.Column<int>(type: "INTEGER", nullable: false)
            .Annotation("Sqlite:Autoincrement", true),
        Title = table.Column<string>(type: "TEXT", nullable: false),
        IsDone = table.Column<bool>(type: "INTEGER", nullable: false)
    },
    constraints: table => { table.PrimaryKey("PK_Tasks", x => x.Id); });
```
**Что внутри `Down()`** — откат: `migrationBuilder.DropTable(name: "Tasks");`

**Шаг 2 — применить миграцию:**
```bash
dotnet ef database update
```
Появился файл **`Lesson05/tasks.db`** (24 КБ). Теперь план **исполнен**.

## Задача 6. Просмотр содержимого базы данных

**В Rider:** справа вкладка **Database** → `+` → **Data Source** → **SQLite** → в поле File выбрать `Lesson05/tasks.db` → (при первом запуске Rider предложит скачать драйвер — Download) → OK. Раскрыть `main → tables → Tasks`. Двойной клик по таблице откроет данные.

**То же самое из терминала** (`sqlite3` встроен в macOS):
```
$ sqlite3 tasks.db ".schema Tasks"
CREATE TABLE "Tasks" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Tasks" PRIMARY KEY AUTOINCREMENT,
    "Title" TEXT NOT NULL,
    "IsDone" INTEGER NOT NULL
);
$ sqlite3 tasks.db "SELECT COUNT(*) FROM Tasks;"
0
$ sqlite3 tasks.db "SELECT MigrationId FROM __EFMigrationsHistory;"
20261012203245_InitialCreate
```
- `Id` — первичный ключ с **автоинкрементом** (база сама выдаёт 1, 2, 3...);
- `Title` — текст (`TEXT`), `IsDone` — в SQLite нет типа bool, поэтому `INTEGER` (0/1);
- таблица **пуста**: данные из старого списка в памяти не переносились;
- служебная таблица `__EFMigrationsHistory` хранит, какие миграции уже применены.

## Задача 7. GET-запросы читают из базы

`GET /api/tasks` → `await db.Tasks.ToListAsync()`, `GET /api/tasks/{id:int}` → `await db.Tasks.FindAsync(id)` (логика 404 сохранена).

**Результат:**
```
GET /api/tasks   → 200 []                                  (пустой массив)
GET /api/tasks/1 → 404 {"error":"Задача не найдена"}
```
**Почему список пуст:** endpoints теперь читают **другой источник** — таблицу `Tasks` в файле `tasks.db`, а она пока пустая (задача 6). Три «стартовые» задачи живут в старом `TaskStorage` в памяти, и Razor-страница `/Tasks` пока показывает их. Два источника данных временно **рассинхронизированы**, это исправит задача 10.

## Задача 8. POST пишет в базу

```csharp
var created = new TaskItem { Title = request.Title.Trim(), IsDone = false }; // Id не задаём
db.Tasks.Add(created);          // объект помечен как новый (Added), в базе его ещё нет
await db.SaveChangesAsync();    // EF выполняет INSERT; база генерирует Id и EF записывает его в created.Id
```
**Проверка:**
```
POST /api/tasks {"title":"Изучить EF Core"} → 201 {"id":1,"title":"Изучить EF Core","isDone":false}
GET  /api/tasks/1                           → 200 (раньше было 404)
sqlite3 tasks.db "SELECT * FROM Tasks;"     → 1|Изучить EF Core|0
```
Данные реально лежат в файле базы.

## Задача 9. DELETE удаляет из базы (+ PUT из ДЗ занятия 1)

```csharp
TaskItem? task = await db.Tasks.FindAsync(id);
if (task == null) return Results.NotFound(...);   // 404, как раньше
db.Tasks.Remove(task);
await db.SaveChangesAsync();                       // DELETE в базе
```
`PUT /api/tasks/{id}` (отметка «выполнено» из ДЗ занятия 1) тоже переведён на базу. Иначе после удаления старого хранилища он бы перестал работать. EF **отслеживает изменения** загруженного объекта: достаточно изменить `task.IsDone` и вызвать `SaveChangesAsync()`, EF сам выполнит `UPDATE`.

**Проверка:**
```
PUT    /api/tasks/1 {"isDone":true} → 200, в базе: 1|Изучить EF Core|1
DELETE /api/tasks/1                 → 200
GET    /api/tasks/1                 → 404 (удалена из базы)
DELETE /api/tasks/999               → 404
```

## Задача 10. Razor-страницы на EF Core, старое хранилище удалено

- `TasksModel` и `StatsModel` получают `TaskDbContext` **через конструктор** (DI):
  ```csharp
  private readonly TaskDbContext _db;
  public TasksModel(TaskDbContext db) { _db = db; }
  ```
- `OnGet` → `OnGetAsync` (`await _db.Tasks.ToListAsync()`), классическое добавление → `OnPostAsync` (`_db.Tasks.Add(...)` + `SaveChangesAsync`), классическое удаление → `OnPostDeleteAsync` (`FindAsync` + `Remove` + `SaveChangesAsync`). Суффикс `Async` в именах обработчиков Razor Pages разрешён и игнорируется при выборе (`handler=Delete` найдёт `OnPostDeleteAsync`).
- `StatsModel` считает прямо в базе: `await _db.Tasks.CountAsync(t => t.IsDone)`. EF превращает это в SQL `SELECT COUNT(*) ... WHERE IsDone`, без загрузки всех задач в память.
- Файл `Data/TaskStorage.cs` **удалён**. Поиск по проекту `TaskStorage` → **0 совпадений**. Единственный источник данных — база через `TaskDbContext`.

**Повтор сценария из задачи 1:**
```
Добавлено: «Через классическую форму» (форма) и «Через AJAX» (API)
БД: 2|Через классическую форму   3|Через AJAX
== перезапуск сервера ==
/Tasks → #2 Через классическую форму, #3 Через AJAX   ✅ данные сохранились
/Stats → Всего: 2, Выполнено: 0, Не выполнено: 2
```
(Id начинаются с 2: задача №1 была удалена в задаче 9. `AUTOINCREMENT` в SQLite не выдаёт номера повторно.)
