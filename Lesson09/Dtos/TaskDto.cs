using Lesson1.WebServer.Models;

namespace Lesson1.WebServer.Dtos;

// === зан9 зад2: DTO для чтения задачи ===
// "конверт" тока с теми данными что уходят клиенту. это НЕ сущность базы:
// нет навигационных свойств (Category, Tasks), поэтому цикла при сериализации не будет,
// категория просто текстом - CategoryName. record - класс для данных, всё задается в конструкторе
//
// по заданию поля: Id, Title, IsDone, CreatedAt, CategoryName
// + добавил CategoryId и DueDate: они есть в модели после дз, а CategoryId нужен клиенту для PUT
public record TaskDto(
    int Id,
    string Title,
    bool IsDone,
    DateTime CreatedAt,
    int? CategoryId,
    string? CategoryName,
    DateTime? DueDate)
{
    // руками перекладываю сущность -> DTO, сам решаю что пойдет в ответ
    // static - вызывается у типа: TaskDto.FromEntity(task)
    // категория должна быть загружена через Include, иначе CategoryName будет null
    public static TaskDto FromEntity(TaskItem task) => new(
        task.Id,
        task.Title,
        task.IsDone,
        task.CreatedAt,
        task.CategoryId,
        task.Category?.Name,
        task.DueDate);
}
