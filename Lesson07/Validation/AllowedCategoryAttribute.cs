using System.ComponentModel.DataAnnotations;
using Lesson1.WebServer.Models;

namespace Lesson1.WebServer.Validation;

// === дз зан6: свой атрибут валидации ===
// наследую ValidationAttribute (от него же [Required] и тд)
// проверяю что категория из разрешенного списка TaskCategories.All, пустая (null) - можно
// вешаю как [AllowedCategory] на TaskItem.Category
public class AllowedCategoryAttribute : ValidationAttribute
{
    public AllowedCategoryAttribute()
        : base("Выберите категорию из списка: " + string.Join(", ", TaskCategories.All))
    {
    }

    // IsValid вызывается при проверке модели. value - значение свойства, true - норм, false - ошибка
    public override bool IsValid(object? value)
    {
        if (value is not string category || string.IsNullOrWhiteSpace(category))
        {
            return true;   // категории нет - это можно
        }

        return TaskCategories.All.Contains(category);
    }
}
