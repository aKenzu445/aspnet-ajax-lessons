using Lesson1.WebServer.Models;
using Microsoft.EntityFrameworkCore;

namespace Lesson1.WebServer.Data;

// === зан5 зад3: DbContext - сеанс работы с базой ===
// через него все запросы к бд и сохранение, наследуется от DbContext из EF Core
public class TaskDbContext : DbContext
{
    // конструктор берет настройки (какая бд, строка подключения) и отдает в DbContext (": base(options)"), настройки в Program.cs
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options) { }

    // DbSet<TaskItem> - это ТАБЛИЦА Tasks: одна строка = один TaskItem
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}
