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
