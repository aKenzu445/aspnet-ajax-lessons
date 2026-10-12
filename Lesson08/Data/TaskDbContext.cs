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

    // зан8 зад2: таблица Categories
    public DbSet<Category> Categories => Set<Category>();

    // OnModelCreating - тут доп настройка модели (Fluent API, стартовые данные)
    // override - переопределяю метод из DbContext
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // === зан8 зад3: стартовые категории (seed) ===
        // HasData - EF запихнет эти строки прям в миграцию (InsertData)
        // Id пишу сам, так EF понимает какие строки уже добавлял
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Работа" },
            new Category { Id = 2, Name = "Учёба" },
            new Category { Id = 3, Name = "Личное" }
        );
    }
}
