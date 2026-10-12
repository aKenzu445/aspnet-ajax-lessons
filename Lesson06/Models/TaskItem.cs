using System.ComponentModel.DataAnnotations;   // атрибуты [Display], [Required] и тд (зан6)

namespace Lesson1.WebServer.Models;

// зан1 зад8: одна задача из списка to-do
// class - "чертеж" объекта, { get; set; } - свойство, можно читать и менять
// зан5 зад3: этот же класс - сущность EF: по нему создается таблица Tasks, свойство = столбец
public class TaskItem
{
    // по соглашению EF свойство Id - ПЕРВИЧНЫЙ КЛЮЧ, база сама дает 1, 2, 3... (автоинкремент)
    public int Id { get; set; }                 // номер задачи
    // Занятие 6, задача 4: [Display] — человеческое имя поля для <label asp-for="...">
    [Display(Name = "Название")]
    public string Title { get; set; } = "";     // текст задачи (по умолчанию пусто)
    public bool IsDone { get; set; }            // сделана или нет (по умолчанию false)

    // зан5 зад11: дата и время создания (новый столбец -> миграция AddCreatedAt)
    public DateTime CreatedAt { get; set; }

    // дз зан5: категория задачи, необязательная
    // string? - может быть null, в базе столбец тоже допускает NULL
    public string? Category { get; set; }
}
