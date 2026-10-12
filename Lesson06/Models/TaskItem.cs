using System.ComponentModel.DataAnnotations;   // атрибуты [Display], [Required] и тд (зан6)
using Lesson1.WebServer.Validation;           // мой атрибут [AllowedCategory] (дз зан6)

namespace Lesson1.WebServer.Models;

// зан1 зад8: одна задача из списка to-do
// class - "чертеж" объекта, { get; set; } - свойство, можно читать и менять
// зан5 зад3: этот же класс - сущность EF: по нему создается таблица Tasks, свойство = столбец
public class TaskItem
{
    // по соглашению EF свойство Id - ПЕРВИЧНЫЙ КЛЮЧ, база сама дает 1, 2, 3... (автоинкремент)
    public int Id { get; set; }                 // номер задачи
    // зан6 зад4: [Display] - нормальное имя поля для <label asp-for>
    // зан6 зад7: DataAnnotations - правила проверки прям на свойстве:
    //   [Required] - обязательно (пусто или одни пробелы - ошибка)
    //   [StringLength] - не длиннее 100, EF тоже это учитывает
    [Display(Name = "Название")]
    [Required(ErrorMessage = "Название обязательно")]
    [StringLength(100, ErrorMessage = "Не более 100 символов")]
    public string Title { get; set; } = "";     // текст задачи (по умолчанию пусто)
    public bool IsDone { get; set; }            // сделана или нет (по умолчанию false)

    // зан5 зад11: дата и время создания (новый столбец -> миграция AddCreatedAt)
    public DateTime CreatedAt { get; set; }

    // дз зан5: категория задачи, необязательная (string?, может быть null)
    // дз зан6: можно тока категории из TaskCategories.All - проверяет мой атрибут [AllowedCategory] (папка Validation)
    [Display(Name = "Категория")]
    [AllowedCategory]
    public string? Category { get; set; }
}
