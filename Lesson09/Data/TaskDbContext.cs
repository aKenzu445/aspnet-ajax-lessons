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

        // === зан8 зад11: что делать с задачами когда удаляют категорию ===
        // Fluent API - настройка связи кодом (когда соглашений не хватает):
        // у задачи ОДНА категория (HasOne), у категории МНОГО задач (WithMany), ключ CategoryId
        // OnDelete(SetNull): удалили категорию -> у её задач CategoryId = NULL, задачи остаются "без категории"
        // (по умолчанию было ClientSetNull и база не давала удалить: FOREIGN KEY constraint failed)
        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
