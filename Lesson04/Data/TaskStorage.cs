using Lesson1.WebServer.Models;

namespace Lesson1.WebServer.Data;

// зад8: хранилище задач в ПАМЯТИ сервака
// static - один общий на всё приложение, все запросы видят один список
// живет пока сервак запущен, после перезапуска опять стартовые задачи
public static class TaskStorage
{
    // стартовые задачи чтоб было не пусто
    private static readonly List<TaskItem> Tasks = new()
    {
        new TaskItem { Id = 1, Title = "Изучить ASP.NET", IsDone = false },
        new TaskItem { Id = 2, Title = "Разобраться с AJAX", IsDone = false },
        new TaskItem { Id = 3, Title = "Сдать задания преподавателю", IsDone = false }
    };

    // зад9: счетчик Id: следующая будет 4, потом 5...
    private static int _nextId = 4;

    // все задачи
    public static List<TaskItem> GetAll() => Tasks;

    // одна задача по Id или null если нету. FirstOrDefault - первый подходящий или null
    public static TaskItem? GetById(int id) => Tasks.FirstOrDefault(t => t.Id == id);

    // зад9: создать задачу с новым Id и закинуть в список
    public static TaskItem Add(string title)
    {
        var task = new TaskItem { Id = _nextId, Title = title, IsDone = false };
        _nextId++;
        Tasks.Add(task);
        return task;
    }

    // зад10: удалить задачу. true - если нашлась и удалилась
    public static bool Delete(int id)
    {
        TaskItem? task = GetById(id);
        if (task == null)
        {
            return false;
        }

        Tasks.Remove(task);
        return true;
    }
}
