using System.ComponentModel.DataAnnotations;

namespace Lesson1.WebServer.Validation;

// === зан7 зад5: свой атрибут - запрещенные слова в названии ===
// задачи со словом "тест" создавались случайно когда проверяли систему, запрещаю
// работает на серваке сам (ModelState / TryValidateObject), а в браузере из коробки НЕТ (зад6):
// для него нет js адаптера как у [Required] и [StringLength]
public class NoForbiddenWordsAttribute : ValidationAttribute
{
    private static readonly string[] ForbiddenWords = { "тест" };

    public NoForbiddenWordsAttribute()
        : base("Название не должно содержать слово 'тест'")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is not string text)
        {
            return true;   // пустое проверяет [Required], а не этот атрибут
        }

        // без учета регистра: "Тест", "ТЕСТ", "тестовая" - всё нельзя (OrdinalIgnoreCase)
        foreach (string word in ForbiddenWords)
        {
            if (text.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
