namespace Lesson1.WebServer.Models;

// зад8: одна задача из списка to-do
// class - "чертеж" объекта, { get; set; } - свойство, можно читать и менять
public class TaskItem
{
    public int Id { get; set; }                 // номер задачи
    public string Title { get; set; } = "";     // текст задачи (по умолчанию пусто)
    public bool IsDone { get; set; }            // сделана или нет (по умолчанию false)
}
