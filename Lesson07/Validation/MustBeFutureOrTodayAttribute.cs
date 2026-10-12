using System.ComponentModel.DataAnnotations;

namespace Lesson1.WebServer.Validation;

// === дз зан7: атрибут "дата не в прошлом" ===
// для срока DueDate: можно сегодня или позже, пусто (null) тоже можно
// как и [NoForbiddenWords] работает на серваке: ModelState (razor) и Validator.TryValidateObject (minimal api)
public class MustBeFutureOrTodayAttribute : ValidationAttribute
{
    public MustBeFutureOrTodayAttribute()
        : base("Срок не может быть в прошлом")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not DateTime date)
        {
            return true;   // null - срока нет, это можно
        }

        // .Date - тока дата без времени, DateTime.Today - сегодня 00:00
        return date.Date >= DateTime.Today;
    }
}
