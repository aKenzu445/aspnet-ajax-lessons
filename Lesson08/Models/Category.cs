namespace Lesson1.WebServer.Models;

// === зан8 зад2: сущность Категория - своя таблица Categories ===
// один-ко-многим: в категории МНОГО задач, у задачи ОДНА категория (или нет)
public class Category
{
    public int Id { get; set; }                 // первичный ключ
    public string Name { get; set; } = "";      // название: "Работа", "Учёба", ...

    // навигационное свойство с другой стороны: все задачи этой категории
    // столбца нет, EF находит их по Tasks.CategoryId
    public List<TaskItem> Tasks { get; set; } = new();
}
