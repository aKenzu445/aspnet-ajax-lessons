namespace Lesson1.WebServer.Models;

// дз зан6: заранее заданный список категорий
// юзается и для <select> в формах, и для проверки (AllowedCategoryAttribute)
public static class TaskCategories
{
    public static readonly string[] All = { "Работа", "Учёба", "Личное", "Спорт" };
}
