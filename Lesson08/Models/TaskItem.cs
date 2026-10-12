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
    //   [StringLength] - от 3 до 100 символов, EF тоже учитывает максимум
    [Display(Name = "Название")]
    [Required(ErrorMessage = "Название обязательно")]
    // зан7 зад4: минимум 3 символа
    [StringLength(100, MinimumLength = 3, ErrorMessage = "От 3 до 100 символов")]
    // зан7 зад5: мой атрибут (папка Validation) - нельзя слово "тест"
    [NoForbiddenWords]
    public string Title { get; set; } = "";     // текст задачи (по умолчанию пусто)
    public bool IsDone { get; set; }            // сделана или нет (по умолчанию false)

    // зан5 зад11: дата и время создания (новый столбец -> миграция AddCreatedAt)
    public DateTime CreatedAt { get; set; }

    // дз зан5: категория задачи, необязательная (string?, может быть null)
    // дз зан6: можно тока категории из TaskCategories.All - проверяет мой атрибут [AllowedCategory] (папка Validation)
    [Display(Name = "Категория")]
    [AllowedCategory]
    public string? Category { get; set; }

    // дз зан7: срок (дедлайн), необязательный. DateTime? - дата которой может не быть (null)
    // [DataType(DataType.Date)] - asp-for нарисует <input type="date">
    // [MustBeFutureOrToday] - мой атрибут: дата не в прошлом
    [Display(Name = "Срок")]
    [DataType(DataType.Date)]
    [MustBeFutureOrToday]
    public DateTime? DueDate { get; set; }
}
